using Application.Abstractions.Anki;

namespace Infrastructure.AnkiPackage;

internal class ApkgGetCardsId(AnkiPackageTransport transport, IAnkiDeckNamesAndIds deckNamesAndIds) : IAnkiCardIds
{
    public async Task<long[]> GetCardsIdAsync(string deckName, CancellationToken token)
    {
        var decks = await deckNamesAndIds.GetDeckNamesAndIdsAsync(token);

        if (!decks.TryGetValue(deckName, out var deckId))
            throw new InvalidOperationException($"Deck '{deckName}' was not found in APKG package.");

        var connection = await transport.GetConnectionAsync(token);
        var cardIds = new List<long>();

        await using var command = connection.CreateCommand();
        command.CommandText = "select id from cards where did = $deckId order by id";
        command.Parameters.AddWithValue("$deckId", deckId);

        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
            cardIds.Add(reader.GetInt64(0));

        return cardIds.ToArray();
    }
}
