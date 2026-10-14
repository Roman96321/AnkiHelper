using System;

namespace Application.UseCases.Training.TranslationSentence
{
    public class TranslationSentenceResponse
    {
        public long NoteId { get; set; }
        public string MoreCorrectSentence { get; set; } = string.Empty;
        public string Verdict { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int RemainingCount { get; set; }
        public string QuestionSentence { get; set; } = string.Empty;
    }
}
