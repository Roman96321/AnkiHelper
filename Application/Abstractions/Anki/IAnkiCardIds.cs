using System;

namespace Application.Abstractions.Anki
{
    public interface IAnkiCardIds
    {
        Task<long[]> GetCardsIdAsync(string deckName, CancellationToken token);
    }
}
