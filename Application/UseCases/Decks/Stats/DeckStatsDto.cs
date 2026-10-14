namespace Application.UseCases.Decks.Stats;

public class DeckStatsDto
{
    public long DeckId { get; set; }
    public string DeckName { get; set; } = string.Empty;
    public int LearnedNotes { get; set; }
    public int WeakNotes { get; set; }
    public int NewNotes { get; set; }
    public int ReviewNotes { get; set; }
}
