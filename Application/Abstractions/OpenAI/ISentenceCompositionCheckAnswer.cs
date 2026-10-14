using Application.UseCases.Training.SentenceComposition;
using System;

namespace Application.Abstractions.OpenAI
{
    public interface ISentenceCompositionCheckAnswer
    {
        Task<SentenceCompositionResponse> CheckAnswer(SentenceCompositionRequest request,CancellationToken token);   
    }
}
