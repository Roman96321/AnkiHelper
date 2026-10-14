using Application.Abstractions;
using Application.Abstractions.OpenAI;
using System;

namespace Application.UseCases.Training.WordOrder
{
    public class WordOrderUseCase(IWordOrderQuestionBuilder wordOrderQuestionBuilder, ITrainingRepository repository)
    {
        public async Task<WordOrderQuestion?> GetQuestionAsync(WordOrderRequest request, Guid userId, CancellationToken token)
        {
            if (request.NoteId != 0)
                await repository.UpdateNote(request.DeckId, request.NoteId, request.IsCorrectAnswer, userId, token);

            var note = await repository.GetNewNoteAsync(request.DeckId, userId, token);

            if (note == null)
            {
                note = await repository.GetReviewAndWeakNoteAsync(request.DeckId, userId, token);

                if (note == null)
                    return null;
            }

            var randomNotes = await repository.GetRandomNotesAsync(request.DeckId, note.Id, Random.Shared.Next(2, 4), userId, token);

            var result = await wordOrderQuestionBuilder.BuildQuestion(note, randomNotes, token);
            result.RemainingCount = await repository.GetRemainingNotesCountAsync(request.DeckId, userId, token);

            return result;
        }
    }
}
