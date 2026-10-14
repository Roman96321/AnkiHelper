using Application.Abstractions;
using Application.Abstractions.OpenAI;
using Domain.Deck;
using System;

namespace Application.UseCases.Training.SentenceComposition
{
    public class SentenceCompositionUseCase(ISentenceCompositionCheckAnswer checkAnswer, ITrainingRepository repository)
    {
        public async Task<SentenceCompositionResponse?> GetQuestionAsync(SentenceCompositionRequest request, Guid userId, CancellationToken token) 
        {
            var result = new SentenceCompositionResponse();
            var hasAnswer = request.NoteId != 0;
            request.UserId = userId;

            if (hasAnswer)
            {
                result = await checkAnswer.CheckAnswer(request, token);
                await repository.UpdateNote(request.DeckId, request.NoteId, result.IsCorrect, userId, token);
            }

            var targetNote = await repository.GetNewNoteAsync(request.DeckId, userId, token);

            if (targetNote == null)
            {
                targetNote = await repository.GetReviewAndWeakNoteAsync(request.DeckId, userId, token);

                if (targetNote == null)
                {
                    if (!hasAnswer)
                        return null;

                    result.RemainingCount = 0;
                    return result;
                }
            }

            var randomNotes = await repository.GetRandomNotesAsync(request.DeckId, targetNote.Id, Random.Shared.Next(1, 3), userId, token);
            List<string> words = new List<string> { targetNote.Front };

            foreach (var item in randomNotes)
                words.Add(item.Front);

            result.Words = words.ToArray();
            result.NoteId = targetNote.Id;
            result.RemainingCount = await repository.GetRemainingNotesCountAsync(request.DeckId, userId, token);

            return result;
        }
    }
}
