using Application.Services.BuildDecks;
using Microsoft.Extensions.Caching.Memory;

namespace Application.UseCases.Decks.DeckImports.PrepareConfiguration
{
    public class PrepareDeckImportConfigurationUseCase(
        IPreparedImportDecksBuilder importDecksBuilder,
        IMemoryCache cache)
    {
        public async Task<List<DeckImportConfiguration>> PrepareConfigurationAsync(List<string> deckNames, Guid userId, CancellationToken token)
        {
            var preparedDecks = await importDecksBuilder.GetPreparedImportDecksAsync(deckNames, token);
            
            var configurationBuilder = new DeckImportConfigurationBuilder();

            cache.Set(DeckImportCacheKeys.ImportDecks(userId), preparedDecks, TimeSpan.FromMinutes(30));

            return configurationBuilder.Build(preparedDecks);
        }    
    }
}
