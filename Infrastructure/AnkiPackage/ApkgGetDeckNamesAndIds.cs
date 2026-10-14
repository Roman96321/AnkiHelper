using Application.Abstractions.Anki;
using System.Text.Json;

namespace Infrastructure.AnkiPackage;

internal class ApkgGetDeckNamesAndIds(AnkiPackageTransport transport) : IAnkiDeckNamesAndIds
{
    public async Task<Dictionary<string, long>> GetDeckNamesAndIdsAsync(CancellationToken token)
    {
        var connection = await transport.GetConnectionAsync(token);
        var decksJson = await AnkiPackageTransport.ReadSingleTextValueAsync(connection, "select decks from col limit 1", token);

        if (!string.IsNullOrWhiteSpace(decksJson))
            return ReadDecksFromJson(decksJson);

        if (!await AnkiPackageTransport.TableExistsAsync(connection, "decks", token))
            throw new InvalidOperationException("Decks JSON is empty and the database does not contain a decks table.");

        var decks = new Dictionary<string, long>();

        await using var command = connection.CreateCommand();
        command.CommandText = "select id, name from decks order by name";

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
            decks[reader.GetString(1)] = reader.GetInt64(0);

        return decks;
    }

    private static Dictionary<string, long> ReadDecksFromJson(string decksJson)
    {
        var decks = new Dictionary<string, long>();

        using var document = JsonDocument.Parse(decksJson);

        foreach (var deckProperty in document.RootElement.EnumerateObject())
        {
            if (!long.TryParse(deckProperty.Name, out var deckId))
                continue;

            if (deckProperty.Value.TryGetProperty("name", out var name))
                decks[name.GetString() ?? deckProperty.Name] = deckId;
        }

        return decks;
    }
}
