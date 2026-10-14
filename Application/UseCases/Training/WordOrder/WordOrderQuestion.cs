using System;

namespace Application.UseCases.Training.WordOrder
{
    public class WordOrderQuestion
    {
        public long NoteId {  get; set; }
        public string[] Words { get; set; } = new string[0];
        public string CorrectSentence { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public int RemainingCount { get; set; }
    }
}
