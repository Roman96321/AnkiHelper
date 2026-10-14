using System;

namespace Application.UseCases.Decks.Settings
{
    public class DeckSettings
    {
        public Dictionary<long, string> NewDeckNames { get; set; } = new Dictionary<long, string>();
        public Dictionary<long, int> DeckDailyNewNotesCount { get; set; } = new Dictionary<long, int>();
        public Dictionary<long, bool> UseOnlyLearnedNotes { get; set; } = new Dictionary<long, bool>();
    }
}
