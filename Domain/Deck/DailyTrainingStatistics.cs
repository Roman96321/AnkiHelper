using System;

namespace Domain.Deck
{
    public class DailyTrainingStatistics
    {
        public Guid UserId { get; set; }
        public DateOnly Date { get; set; }
        public int LearningNoteCounts { get; set; }
    }
}