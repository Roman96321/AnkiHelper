namespace Application.Services.ConfiguredDecks;

public class DeckImportSelection
{
    public long DeckId { get; set; }
    public string DeckName { get; set; } = string.Empty;
    public List<DeckImportFieldSelection> FieldMappings { get; set; } = new();
}

public class DeckImportFieldSelection
{
    public string FrontKey { get; set; } = string.Empty;
    public string BackKey { get; set; } = string.Empty;
}
