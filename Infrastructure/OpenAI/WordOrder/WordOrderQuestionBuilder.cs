using Application.Abstractions;
using Application.Abstractions.OpenAI;
using Application.UseCases.Training.WordOrder;
using Domain.Deck;
using System;
using System.Text.Json;

namespace Infrastructure.OpenAI.WordOrder
{
    public class WordOrderQuestionBuilder(OpenAITransport transport, IDeckRepository repository) : IWordOrderQuestionBuilder
    {
        private const string Instruction = """
            You create one natural short sentence for a word-order language exercise.
            Return valid JSON only. Do not use markdown.

            JSON schema:
            {
              "sentence": "short natural sentence in the source language",
              "translation": "translation in the target language"
            }

            Rules:
            - The sentence must include the exact target word.
            - Use a simple everyday sentence.
            - You may use some optional vocabulary if it helps, but do not force unnatural grammar.
            - Keep the sentence between 4 and 10 words.
            - The translation must preserve the sentence meaning.
            """;

        public async Task<WordOrderQuestion> BuildQuestion(Note trainingNote, List<Note> notes, CancellationToken token)
        {
            var optionalWords = notes
                .Select(note => note.Front)
                .ToArray();

            var language = await repository.GetDeckLanguage(trainingNote.DeckId, trainingNote.UserId, token);
            var prompt = $"""
                Source language: {language.FrontLanguage}
                Target translation language: {language.BackLanguage}
                Target word: {trainingNote.Front}
                Target word translation: {trainingNote.Back}
                Optional vocabulary: {string.Join(", ", optionalWords)}
                """;

            var generatedQuestion = await GenerateQuestionAsync(prompt, trainingNote, token);

            var randomizeWords = generatedQuestion.Sentence.ToLower().Replace(".", "").Split(" ", StringSplitOptions.RemoveEmptyEntries);
            randomizeWords.Shuffle();

            return new WordOrderQuestion
            {
                NoteId = trainingNote.Id,
                Words = randomizeWords,
                CorrectSentence = generatedQuestion.Sentence.Replace(".", ""),
                Translation = generatedQuestion.Translation,
            };
        }

        private async Task<GeneratedWordOrderQuestion> GenerateQuestionAsync(string prompt, Note trainingNote, CancellationToken token)
        {
            try
            {
                var response = await transport.SendResponseAsync(prompt, Instruction, token);
                return ParseQuestion(response, trainingNote);
            }
            catch (InvalidOperationException)
            {
                return CreateFallbackQuestion(trainingNote);
            }
        }

        private GeneratedWordOrderQuestion ParseQuestion(string response, Note trainingNote)
        {
            try
            {
                var question = JsonSerializer.Deserialize<GeneratedWordOrderQuestion>(
                    response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!string.IsNullOrWhiteSpace(question?.Sentence))
                    return new GeneratedWordOrderQuestion(
                        question.Sentence.Trim(),
                        question.Translation?.Trim() ?? string.Empty);
            }
            catch (JsonException)
            {
            }

            var responseParts = response.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (responseParts.Length > 0 && !string.IsNullOrWhiteSpace(responseParts[0]))
                return new GeneratedWordOrderQuestion(
                    responseParts[0].Trim(),
                    responseParts.Length > 1 ? responseParts[1].Trim() : string.Empty);

            return CreateFallbackQuestion(trainingNote);
        }

        private GeneratedWordOrderQuestion CreateFallbackQuestion(Note trainingNote)
        {
            return new GeneratedWordOrderQuestion(trainingNote.Front, trainingNote.Back);
        }

        private sealed record GeneratedWordOrderQuestion(string Sentence, string Translation);
    }

    public static class ArrayExtensions
    {
        public static void Shuffle<T>(this T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);

                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
