using Application.UseCases.Training.WordOrder;
using Domain.Deck;
using System;


namespace Application.Abstractions.OpenAI
{
    public interface IWordOrderQuestionBuilder
    {
        Task<WordOrderQuestion> BuildQuestion(Note trainingNote, List<Note> notes, CancellationToken token);
    }
}
