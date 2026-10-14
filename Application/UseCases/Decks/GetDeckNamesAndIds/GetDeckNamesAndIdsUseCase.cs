using Application.Abstractions.Anki;
using System;


namespace Application.UseCases.Decks.GetDeckNamesAndIds
{
    public class GetDeckNamesAndIdsUseCase(IAnkiDeckNamesAndIds deckNamesAndIds)
    {
        public async Task<Dictionary<string, long>> GetDeckNamesAndIdsAsync(CancellationToken token)
        {
            return await deckNamesAndIds.GetDeckNamesAndIdsAsync(token);
        }
    }
}
