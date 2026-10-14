using System;

namespace Domain.Deck
{
    public class Note
    {
        public Guid UserId { get; set; }
        public long Id { get; set; }
        public long DeckId { get; set; }
        public string Front { get; set; } = null!;
        public string Back { get; set; } = null!;
        public bool IsLearned { get; set; }
        public int LearningPoints { get; set; }
        public DateOnly NextReviewDate { get; set; }
    }
}