namespace Application.UseCases.Decks.DeckImports.PrepareConfiguration
{
    public class DeckImportConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public List<DeckImportNoteConfiguration> Notes { get; set; } = new List<DeckImportNoteConfiguration>();
    }

    public class DeckImportNoteConfiguration
    {
        public int Id { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }
}
