using Application.UseCases.Decks.DeckImports.Models;

namespace Application.Abstractions.Anki
{
    public interface IAnkiCardsInfo
    {
        Task<List<AnkiCardInfo>> GetCardsInfoAsync(long[] cardsId, CancellationToken token);
    }
}
