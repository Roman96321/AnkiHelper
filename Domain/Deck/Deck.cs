using System;

namespace Domain.Deck
{
    public class Deck
    {
        public Guid UserId { get; set; }
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Note> Notes { get; set; } = new List<Note>();
        public string FrontLanguage { get; set; } = null!;
        public string BackLanguage { get; set; } = null!;
        public int DeckDailyNewNotesCount { get; set; } 
        public bool UseOnlyLearnedNotes { get; set; }
    }
}