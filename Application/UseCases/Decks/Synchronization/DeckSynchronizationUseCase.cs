using Application.Abstractions;
using Application.Abstractions.Anki;
using Application.Services.BuildDecks;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.DeckImports.Models;
using Domain.Deck;

namespace Application.UseCases.Decks.Synchronization
{
    public class DeckSynchronizationUseCase(
        IDeckRepository repository, 
        IPreparedImportDecksBuilder importDecksBuilder,
        IConfiguredDeckImportBuilder configuredDeckImportBuilder,
        IAnkiDeckNamesAndIds getDecksId)
    {
        private readonly DeckNamesVerification _namesVerification = new DeckNamesVerification();

        public async Task SynchronizeDecksAsync(Guid userId, CancellationToken token)
        {
            var oldDeckNames = await repository.GetAllDeckNamesAndIdsAsync(userId, token);

            if (oldDeckNames.Count == 0)
                return;

            var newDeckNames = await getDecksId.GetDeckNamesAndIdsAsync(token);
            var verifiedDeckNames = _namesVerification.Verify(oldDeckNames, newDeckNames);

            var preparedDecks = await importDecksBuilder.GetPreparedImportDecksAsync(verifiedDeckNames, token);

            var decksSettings = await repository.GetDeckImportSettingsAsync(preparedDecks.Select(d => d.Id).ToList(), userId, token);

            var deckImportSelections = BuildDeckImportSelections(decksSettings, preparedDecks);
            var result = await configuredDeckImportBuilder.Build(deckImportSelections, preparedDecks, token);

            await repository.SaveDeckImportSettingsAsync(deckImportSelections, userId, token);
            await repository.ReplaceAllAsync(result, userId, token);
        }

        private List<DeckImportSelection> BuildDeckImportSelections(
            List<DeckImportSettings> deckImportSettings,
            List<PreparedImportDeck> preparedDecks)
        {
            return deckImportSettings.Select(settings =>
            {
                var preparedDeck = preparedDecks.Single(deck => deck.Id == settings.DeckId);

                return new DeckImportSelection
                {
                    DeckId = settings.DeckId,
                    DeckName = preparedDeck.Name,
                    FieldMappings = settings.FieldMappings.Select(fieldMapping => new DeckImportFieldSelection
                    {
                        FrontKey = fieldMapping.FrontKey,
                        BackKey = fieldMapping.BackKey
                    }).ToList()
                };
            }).ToList();
        }
    }
}
