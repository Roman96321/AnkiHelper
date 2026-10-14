namespace Application.UseCases.Decks.DeckImports.ImportConfigured
{
    public class ConfiguredDeckImport
    {
        public long DeckId { get; set; }
        public string DeckName { get; set; } = string.Empty;
        public List<ConfiguredNoteImport> NoteTypes { get; set; } = new List<ConfiguredNoteImport>();
    }

    public class ConfiguredNoteImport
    {
        public string FrontKey { get; set; } = string.Empty;
        public string BackKey { get; set; } = string.Empty;
    }
}
