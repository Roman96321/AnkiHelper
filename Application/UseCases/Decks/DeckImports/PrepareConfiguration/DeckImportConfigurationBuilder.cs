using Application.UseCases.Decks.DeckImports.Models;

namespace Application.UseCases.Decks.DeckImports.PrepareConfiguration
{
    public class DeckImportConfigurationBuilder
    {
        public List<DeckImportConfiguration> Build(List<PreparedImportDeck> importDecks)
        {
            var result = new List<DeckImportConfiguration>();

            foreach (var deck in importDecks)
            {
                var configureDeck = new DeckImportConfiguration { Name = deck.Name };

                var noteTypes = deck.Notes
                    .GroupBy(note => BuildFieldsSignature(note))
                    .Select(group => group.First());

                foreach (var note in noteTypes)
                {
                    var configureNote = new DeckImportNoteConfiguration { Id = configureDeck.Notes.Count + 1 };

                    foreach (var field in note.Fields.OrderBy(field => field.Value.Order))
                        configureNote.Fields.TryAdd(field.Key, AnkiFieldTextCleaner.Clean(field.Value.Value));

                    configureDeck.Notes.Add(configureNote);
                }

                result.Add(configureDeck);
            }

            return result;
        }

        private static string BuildFieldsSignature(PreparedImportNote note)
        {
            return string.Join('|', note.Fields
                .OrderBy(field => field.Value.Order)
                .Select(field => field.Key));
        }
    }
}
