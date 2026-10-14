using Application.UseCases.Decks.DeckImports.Models;
using Application.Abstractions.Anki;

namespace Infrastructure.AnkiPackage;

internal class ApkgGetCardsInfo(AnkiPackageTransport transport) : IAnkiCardsInfo
{
    public async Task<List<AnkiCardInfo>> GetCardsInfoAsync(long[] cardsId, CancellationToken token)
    {
        var result = new List<AnkiCardInfo>();

        foreach (var cardIdChunk in cardsId.Chunk(500))
        {
            token.ThrowIfCancellationRequested();

            var connection = await transport.GetConnectionAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                select nid, queue
                from cards
                where id in ({AnkiPackageTransport.BuildInParameters(command, "$card", cardIdChunk)})
                order by id;
                """;

            await using var reader = await command.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                result.Add(new AnkiCardInfo
                {
                    NoteId = reader.GetInt64(0),
                    Queue = reader.GetInt32(1)
                });
            }
        }

        return result;
    }
}
