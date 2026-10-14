using Application.Abstractions.Anki;
using Application.UseCases.Decks.DeckImports.Models;
using Infrastructure.AnkiConnect.DTOs;

namespace Infrastructure.AnkiConnect
{
    internal class AnkiGetCardsInfo(AnkiConnectTransport transport) : IAnkiCardsInfo
    {
        public async Task<List<AnkiCardInfo>> GetCardsInfoAsync(long[] cardsId, CancellationToken token)
        {
            var result = new List<AnkiCardInfo>();

            foreach (var card in cardsId.Chunk(300))
            {
                token.ThrowIfCancellationRequested();

                var chunkedCardsId = card.ToList();
                var resultJson = await transport.SendRequestAsync<List<CardsInfoDto>>("cardsInfo", token, new { cards = chunkedCardsId });

                foreach (var dtoCard in resultJson)
                {
                    result.Add(new AnkiCardInfo
                    {
                        NoteId = dtoCard.Note,
                        Queue = dtoCard.Queue
                    });
                }
            }

            return result;
        }
    }
}
