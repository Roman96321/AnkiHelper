namespace Application.UseCases.Decks.DeckImports.Models
{
    public class PreparedImportDeck
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public List<PreparedImportNote> Notes { get; set; } = new List<PreparedImportNote>();
    }

    public class PreparedImportNote
    {
        public long NoteId { get; set; }
        public bool IsLearned { get; set; }
        public Dictionary<string, AnkiField> Fields { get; set; } = null!;
    }

    public class AnkiField
    {
        public string Value { get; set; } = null!;
        public int Order { get; set; }
    }
}
