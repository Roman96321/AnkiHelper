#pragma warning disable OPENAI001

using OpenAI.Responses;
using OpenAI;
using Microsoft.Extensions.Configuration;
using System;

namespace Infrastructure.OpenAI
{   
    public class OpenAITransport
    {
        private const int MaxOutputTokenCount = 600;

        private readonly ResponsesClient _client;
        private readonly string _model;

        public OpenAITransport(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is not configured. Set OpenAI:ApiKey or OPENAI_API_KEY.");

            _model = configuration["OpenAI:Model"] ?? "gpt-5.4-mini-2026-03-17";
            _client = new ResponsesClient(apiKey: apiKey);
        }

        public async Task<string> SendResponseAsync(string prompt, string instruction, CancellationToken token, string? model = null)
        {
            var options = new CreateResponseOptions
            {
                Model =  model ?? _model,
                Instructions = instruction,
                MaxOutputTokenCount = MaxOutputTokenCount,
                ReasoningOptions = new ResponseReasoningOptions
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.None
                }
            };

            options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

            ResponseResult res = await _client.CreateResponseAsync(options, token);
            var result = res.GetOutputText().Trim();

            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("OpenAI response did not contain text output.");

            return result;
        }
    }
}
