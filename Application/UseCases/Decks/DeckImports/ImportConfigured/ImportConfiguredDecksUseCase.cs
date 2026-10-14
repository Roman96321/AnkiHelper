using Application.Abstractions;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.DeckImports.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Application.UseCases.Decks.DeckImports.ImportConfigured
{
    public class ImportConfiguredDecksUseCase(
        IMemoryCache memoryCache, 
        IDeckRepository repository, 
        IConfiguredDeckImportBuilder configuredDeckImportBuilder)
    {
        public async Task ImportAsync(List<ConfiguredDeckImport> configuredDecks, Guid userId, CancellationToken token)
        {
            var importableConfiguredDecks = configuredDecks
                .Where(deck => deck.NoteTypes.Count > 0)
                .ToList();

            if (importableConfiguredDecks.Count == 0)
                throw new InvalidOperationException("At least one deck import configuration must contain note types.");

            var preparedDecks = memoryCache.Get<List<PreparedImportDeck>>(DeckImportCacheKeys.ImportDecks(userId));

            if (preparedDecks == null)
                throw new InvalidOperationException("No decks to import. Please prepare the decks before importing.");

            var deckImportSelections = BuildDeckImportSelections(importableConfiguredDecks, preparedDecks);
            var result = await configuredDeckImportBuilder.Build(deckImportSelections, preparedDecks, token);

            await repository.SaveDeckImportSettingsAsync(deckImportSelections, userId, token);
            await repository.ReplaceAsync(result, userId, token);
        }

        private List<DeckImportSelection> BuildDeckImportSelections(
            List<ConfiguredDeckImport> configuredDecks,
            List<PreparedImportDeck> preparedDecks)
        {
            return configuredDecks.Select(configuredDeck =>
            {
                var preparedDeck = configuredDeck.DeckId == 0
                    ? preparedDecks.Single(deck => deck.Name == configuredDeck.DeckName)
                    : preparedDecks.Single(deck => deck.Id == configuredDeck.DeckId);

                return new DeckImportSelection
                {
                    DeckId = preparedDeck.Id,
                    DeckName = preparedDeck.Name,
                    FieldMappings = configuredDeck.NoteTypes.Select(noteType => new DeckImportFieldSelection
                    {
                        FrontKey = noteType.FrontKey,
                        BackKey = noteType.BackKey
                    }).ToList()
                };
            }).ToList();
        }
    }
}
