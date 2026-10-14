namespace Application.UseCases.Decks.DeckImports.Models
{
    public class AnkiNoteInfo
    {
        public long NoteId { get; set; }
        public Dictionary<string, AnkiField> Fields { get; set; } = new();
    }
}
