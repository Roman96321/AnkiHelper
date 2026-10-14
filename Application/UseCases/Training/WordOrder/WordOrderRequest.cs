using System;

namespace Application.UseCases.Training.WordOrder
{
    public class WordOrderRequest
    {
        public long DeckId { get; set; }
        public long NoteId { get; set; } = 0;
        public bool IsCorrectAnswer { get; set; }
    }
}
