using System;

namespace Domain.Deck
{
    public class DailyNewNoteProgress
    {
        public Guid UserId { get; set; }
        public long DeckId { get; set; }
        public DateOnly Date { get; set; }
        public int NewAnsweredCount { get; set; }
    }
}
