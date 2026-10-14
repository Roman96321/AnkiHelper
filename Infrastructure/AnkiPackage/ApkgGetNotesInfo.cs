using Application.UseCases.Decks.DeckImports.Models;
using Application.Abstractions.Anki;
using System.Text.Json;

namespace Infrastructure.AnkiPackage;

internal class ApkgGetNotesInfo(AnkiPackageTransport transport) : IAnkiNotesInfo
{
    private Dictionary<long, NoteTypeInfo>? _noteTypes;

    public async Task<List<AnkiNoteInfo>> GetNotesInfoAsync(long[] noteIds, CancellationToken token)
    {
        var result = new List<AnkiNoteInfo>();
        var noteTypes = await GetNoteTypesAsync(token);

        foreach (var noteIdChunk in noteIds.Chunk(500))
        {
            token.ThrowIfCancellationRequested();

            var connection = await transport.GetConnectionAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                select id, mid, flds
                from notes
                where id in ({AnkiPackageTransport.BuildInParameters(command, "$note", noteIdChunk)})
                order by id;
                """;

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var noteId = reader.GetInt64(0);
                var noteTypeId = reader.GetInt64(1);
                var fieldValues = reader.GetString(2).Split('\u001f');
                var noteType = noteTypes.GetValueOrDefault(noteTypeId, new NoteTypeInfo($"Unknown note type {noteTypeId}", []));

                result.Add(new AnkiNoteInfo
                {
                    NoteId = noteId,
                    Fields = BuildFields(noteType, fieldValues)
                });
            }
        }

        return result;
    }

    private async Task<Dictionary<long, NoteTypeInfo>> GetNoteTypesAsync(CancellationToken token)
    {
        if (_noteTypes is not null)
            return _noteTypes;

        var connection = await transport.GetConnectionAsync(token);
        var modelsJson = await AnkiPackageTransport.ReadSingleTextValueAsync(connection, "select models from col limit 1", token);

        _noteTypes = !string.IsNullOrWhiteSpace(modelsJson)
            ? ReadNoteTypesFromJson(modelsJson)
            : await ReadNoteTypesFromTablesAsync(token);

        return _noteTypes;
    }

    private static Dictionary<long, NoteTypeInfo> ReadNoteTypesFromJson(string modelsJson)
    {
        var noteTypes = new Dictionary<long, NoteTypeInfo>();

        using var document = JsonDocument.Parse(modelsJson);

        foreach (var modelProperty in document.RootElement.EnumerateObject())
        {
            if (!long.TryParse(modelProperty.Name, out var noteTypeId))
                continue;

            var model = modelProperty.Value;
            var name = model.TryGetProperty("name", out var nameProperty)
                ? nameProperty.GetString() ?? modelProperty.Name
                : modelProperty.Name;

            var fields = new List<string>();

            if (model.TryGetProperty("flds", out var fieldsProperty))
            {
                foreach (var field in fieldsProperty.EnumerateArray())
                {
                    if (field.TryGetProperty("name", out var fieldName))
                        fields.Add(fieldName.GetString() ?? string.Empty);
                }
            }

            noteTypes[noteTypeId] = new NoteTypeInfo(name, fields);
        }

        return noteTypes;
    }

    private async Task<Dictionary<long, NoteTypeInfo>> ReadNoteTypesFromTablesAsync(CancellationToken token)
    {
        var connection = await transport.GetConnectionAsync(token);

        if (!await AnkiPackageTransport.TableExistsAsync(connection, "notetypes", token) ||
            !await AnkiPackageTransport.TableExistsAsync(connection, "fields", token))
            throw new InvalidOperationException("Models JSON is empty and the database does not contain notetypes/fields tables.");

        var noteTypes = new Dictionary<long, NoteTypeInfo>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select id, name from notetypes order by id";

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
                noteTypes[reader.GetInt64(0)] = new NoteTypeInfo(reader.GetString(1), []);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select ntid, name from fields order by ntid, ord";

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                var noteTypeId = reader.GetInt64(0);
                var fieldName = reader.GetString(1);

                if (noteTypes.TryGetValue(noteTypeId, out var noteType))
                    noteType.Fields.Add(fieldName);
            }
        }

        return noteTypes;
    }

    private static Dictionary<string, AnkiField> BuildFields(NoteTypeInfo noteType, string[] fieldValues)
    {
        var fields = new Dictionary<string, AnkiField>();

        for (var i = 0; i < fieldValues.Length; i++)
        {
            var fieldName = i < noteType.Fields.Count ? noteType.Fields[i] : $"Field {i + 1}";

            fields[fieldName] = new AnkiField
            {
                Value = fieldValues[i],
                Order = i
            };
        }

        return fields;
    }

    private sealed record NoteTypeInfo(string Name, List<string> Fields);
}
