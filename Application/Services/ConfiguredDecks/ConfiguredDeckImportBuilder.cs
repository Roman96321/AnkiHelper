using Application.Abstractions.OpenAI;
using Application.UseCases.Decks.DeckImports;
using Application.UseCases.Decks.DeckImports.Models;
using Domain.Deck;

namespace Application.Services.ConfiguredDecks
{
    public class ConfiguredDeckImportBuilder(IDetermineLanguage determineLanguage) : IConfiguredDeckImportBuilder
    {
        private readonly int _deckDailyNewNotesCount = 10;

        public async Task<List<Deck>> Build(List<DeckImportSelection> deckImportSelections, List<PreparedImportDeck> preparedDecks, CancellationToken token)
        {
            var result = new List<Deck>();
            var importedNoteIds = new HashSet<long>();

            foreach (var deckImportSelection in deckImportSelections)
            {
                var preparedDeck = deckImportSelection.DeckId == 0
                    ? preparedDecks.Single(x => x.Name == deckImportSelection.DeckName)
                    : preparedDecks.Single(x => x.Id == deckImportSelection.DeckId);



                var deck = new Deck
                {
                    Id = preparedDeck.Id,
                    Name = preparedDeck.Name,
                    DeckDailyNewNotesCount = _deckDailyNewNotesCount,
                    UseOnlyLearnedNotes = true
                };

                foreach (var fieldMapping in deckImportSelection.FieldMappings.DistinctBy(fieldMapping => new { fieldMapping.FrontKey, fieldMapping.BackKey }))
                {
                    var notes = preparedDeck.Notes.Where(x =>
                        x.Fields.ContainsKey(fieldMapping.FrontKey) &&
                        x.Fields.ContainsKey(fieldMapping.BackKey));

                    foreach (var note in notes)
                    {
                        if (!importedNoteIds.Add(note.NoteId))
                            continue;

                        deck.Notes.Add(new Note
                        {
                            DeckId = deck.Id,
                            Id = note.NoteId,
                            IsLearned = note.IsLearned,
                            Front = AnkiFieldTextCleaner.Clean(note.Fields[fieldMapping.FrontKey].Value),
                            Back = AnkiFieldTextCleaner.Clean(note.Fields[fieldMapping.BackKey].Value),
                        });
                    }
                }

                deck.FrontLanguage = await DetermineLanguageOrUnknownAsync(
                    deck.Notes.Select(x => x.Front).Take(100).ToArray(),
                    token);

                deck.BackLanguage = await DetermineLanguageOrUnknownAsync(
                    deck.Notes.Select(x => x.Back).Take(100).ToArray(),
                    token);

                result.Add(deck);
            }

            return result;
        }

        private async Task<string> DetermineLanguageOrUnknownAsync(string[] words, CancellationToken token)
        {
            if (words.Length == 0)
                return "und";

            return await determineLanguage.Determine(words, token);
        }
    }
}
