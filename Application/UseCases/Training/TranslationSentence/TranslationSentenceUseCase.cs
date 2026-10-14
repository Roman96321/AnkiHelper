using Application.Abstractions;
using Application.Abstractions.OpenAI;

namespace Application.UseCases.Training.TranslationSentence
{
    public class TranslationSentenceUseCase(ITrainingRepository repository, ITranslationSentenceExerciseService exerciseService)
    {
        public async Task<TranslationSentenceResponse?> GetQuestionAsync(TranslationSentenceRequest request, Guid userId, CancellationToken token)
        {
            var result = new TranslationSentenceResponse();
            var hasAnswer = request.NoteId != 0;
            request.UserId = userId;

            if (hasAnswer)
            {
                result = await exerciseService.CheckAnswer(request, token);
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

            var randomNotes = await repository.GetRandomNotesAsync(request.DeckId, targetNote.Id, 4, userId, token);
            List<string> words = new List<string>();

            foreach (var note in randomNotes)
                words.Add(note.Back);

            result.QuestionSentence = await exerciseService.CreateQuestion(words.ToArray(), targetNote.Back, request.DeckId, userId, token);
            result.NoteId = targetNote.Id;
            result.RemainingCount = await repository.GetRemainingNotesCountAsync(request.DeckId, userId, token);

            return result;
        }
    }
}
