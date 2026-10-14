using System;

namespace Application.UseCases.Training.TranslationSentence
{
    public class TranslationSentenceRequest
    {
        public Guid UserId { get; set; }
        public long DeckId { get; set; }
        public long NoteId { get; set; } = 0;
        public string AnswerSentence { get; set; } = string.Empty;
        public string QuestionSentence {  get; set; } = string.Empty;
    }
}
