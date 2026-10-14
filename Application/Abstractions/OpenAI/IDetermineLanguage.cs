using System;

namespace Application.Abstractions.OpenAI
{
    public interface IDetermineLanguage
    {
        Task<string> Determine(string[] words, CancellationToken token);
    }
}
