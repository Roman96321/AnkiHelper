using Application.UseCases.Training.TranslationSentence;

namespace Application.Abstractions.OpenAI
{
    public interface ITranslationSentenceExerciseService
    {
        Task<TranslationSentenceResponse> CheckAnswer(TranslationSentenceRequest request, CancellationToken token);

        Task<string> CreateQuestion(string[] words, string targetNote, long deckId, Guid userId, CancellationToken token);
    }
}
