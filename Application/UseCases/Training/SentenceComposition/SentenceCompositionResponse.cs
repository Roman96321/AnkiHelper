using System;

namespace Application.UseCases.Training.SentenceComposition
{
    public class SentenceCompositionResponse
    {
        public long NoteId { get; set; }
        public string[] Words { get; set; } = new string[0];
        public string MoreCorrectSentence { get; set; } = string.Empty;
        public string Verdict { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int RemainingCount { get; set; }
    }
}
