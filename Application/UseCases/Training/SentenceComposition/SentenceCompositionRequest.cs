using System;

namespace Application.UseCases.Training.SentenceComposition
{
    public class SentenceCompositionRequest
    {
        public Guid UserId { get; set; }
        public long DeckId { get; set; }
        public long NoteId { get; set; } = 0;
        public string AnswerSentence {  get; set; } = string.Empty;
        public string[] Words {  get; set; } = new string[0];
    }
}
