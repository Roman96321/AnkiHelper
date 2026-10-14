using Application.Abstractions.Auth;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using System.Text;
using ZstdSharp;

namespace Infrastructure.AnkiPackage;

public sealed class AnkiPackageTransport : IDisposable
{
    private const string SqliteHeader = "SQLite format 3";

    private readonly IMemoryCache _memoryCache;
    private readonly ICurrentUser _currentUser;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);

    private SqliteConnection? _connection;
    private string? _tempDirectory;

    public AnkiPackageTransport(IMemoryCache memoryCache, ICurrentUser currentUser)
    {
        SQLitePCL.Batteries.Init();
        _memoryCache = memoryCache;
        _currentUser = currentUser;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connectionGate.Dispose();

        if (_tempDirectory is null)
        {
            return;
        }

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // SQLite can keep a file handle alive briefly; this is harmless for the debug APKG flow.
        }
    }

    internal async Task<SqliteConnection> GetConnectionAsync(CancellationToken token)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _connectionGate.WaitAsync(token);

        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var cacheKey = AnkiPackageCacheKeys.GetAnkiPackageCacheKey(_currentUser.UserId);

            if (!_memoryCache.TryGetValue<byte[]>(cacheKey, out var packageBytes) || packageBytes is null)
            {
                throw new InvalidOperationException("APKG package was not uploaded.");
            }

            _tempDirectory = Path.Combine(Path.GetTempPath(), "anki-helper-apkg", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            var collectionPath = ExtractCollectionDatabase(packageBytes, _tempDirectory);

            _connection = new SqliteConnection($"Data Source={collectionPath};Mode=ReadOnly;Pooling=False");
            await _connection.OpenAsync(token);
            _connection.CreateCollation("unicase", (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));

            return _connection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private static string ExtractCollectionDatabase(byte[] packageBytes, string tempDirectory)
    {
        using var packageStream = new MemoryStream(packageBytes);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

        var collectionEntries = archive.Entries
            .Where(entry =>
                entry.Name.Equals("collection.anki2", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Equals("collection.anki21", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Equals("collection.anki21b", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => GetCollectionEntryPriority(entry.Name))
            .ToList();

        if (collectionEntries.Count == 0)
        {
            throw new InvalidOperationException("The package does not contain collection.anki2, collection.anki21, or collection.anki21b.");
        }

        var diagnostics = new List<string>();

        foreach (var collectionEntry in collectionEntries)
        {
            var entryBytes = ReadEntryBytes(collectionEntry);
            diagnostics.Add($"{collectionEntry.FullName}: {entryBytes.Length} bytes, header {FormatHeader(entryBytes)}");

            if (IsSqliteDatabase(entryBytes))
            {
                return WriteCollectionDatabase(tempDirectory, collectionEntry.Name, entryBytes);
            }

            if (!IsZstdFrame(entryBytes))
            {
                continue;
            }

            if (TryDecompressZstd(entryBytes, out var decompressedBytes, out var zstdError))
            {
                diagnostics.Add($"{collectionEntry.FullName}: zstd decompressed to {decompressedBytes.Length} bytes, header {FormatHeader(decompressedBytes)}");

                if (IsSqliteDatabase(decompressedBytes))
                {
                    return WriteCollectionDatabase(tempDirectory, Path.GetFileNameWithoutExtension(collectionEntry.Name) + ".sqlite", decompressedBytes);
                }
            }
            else
            {
                diagnostics.Add($"{collectionEntry.FullName}: zstd decompression failed: {zstdError}");
            }
        }

        throw new InvalidOperationException(
            "Could not find a SQLite collection database in the package." + Environment.NewLine +
            "Checked entries:" + Environment.NewLine +
            string.Join(Environment.NewLine, diagnostics));
    }

    internal static string BuildInParameters(SqliteCommand command, string prefix, IReadOnlyList<long> values)
    {
        var parameterNames = new List<string>();

        for (var i = 0; i < values.Count; i++)
        {
            var parameterName = $"{prefix}{i}";
            command.Parameters.AddWithValue(parameterName, values[i]);
            parameterNames.Add(parameterName);
        }

        return string.Join(", ", parameterNames);
    }

    internal static async Task<string> ReadSingleTextValueAsync(SqliteConnection connection, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return (await command.ExecuteScalarAsync(token))?.ToString()
            ?? throw new InvalidOperationException($"Query returned empty value: {sql}");
    }

    internal static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from sqlite_master
            where type = 'table' and name = $tableName;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);

        return Convert.ToInt32(await command.ExecuteScalarAsync(token)) > 0;
    }

    private static int GetCollectionEntryPriority(string entryName)
    {
        return entryName.ToLowerInvariant() switch
        {
            "collection.anki21b" => 0,
            "collection.anki21" => 1,
            "collection.anki2" => 2,
            _ => 3
        };
    }

    private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var memoryStream = new MemoryStream();

        entryStream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    private static string WriteCollectionDatabase(string tempDirectory, string fileName, byte[] databaseBytes)
    {
        var collectionPath = Path.Combine(tempDirectory, fileName);
        File.WriteAllBytes(collectionPath, databaseBytes);

        return collectionPath;
    }

    private static bool IsSqliteDatabase(byte[] bytes)
    {
        var headerBytes = Encoding.ASCII.GetBytes(SqliteHeader);

        return bytes.Length >= headerBytes.Length && bytes.Take(headerBytes.Length).SequenceEqual(headerBytes);
    }

    private static bool IsZstdFrame(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == 0x28 &&
               bytes[1] == 0xB5 &&
               bytes[2] == 0x2F &&
               bytes[3] == 0xFD;
    }

    private static bool TryDecompressZstd(byte[] compressedBytes, out byte[] decompressedBytes, out string? error)
    {
        try
        {
            using var decompressor = new Decompressor();
            decompressedBytes = decompressor.Unwrap(compressedBytes).ToArray();
            error = null;

            return true;
        }
        catch (Exception exception)
        {
            decompressedBytes = [];
            error = exception.Message;

            return false;
        }
    }

    private static string FormatHeader(byte[] bytes)
    {
        var headerLength = Math.Min(bytes.Length, 16);

        return BitConverter.ToString(bytes, 0, headerLength);
    }
}
