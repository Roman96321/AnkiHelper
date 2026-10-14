using Application.Abstractions;
using Application.Abstractions.OpenAI;
using Application.UseCases.Training.TranslationSentence;
using System.Text.Json;

namespace Infrastructure.OpenAI.TranslationSentence
{
    internal class TranslationSentenceExerciseService(OpenAITransport transport, IDeckRepository repository) : ITranslationSentenceExerciseService
    {
        private const string AnswerInstruction = """
            You are checking a translation exercise.
            The user receives a sentence in the source language and writes its translation in the target language.
            Evaluate whether the user's translation preserves the meaning of the source sentence and sounds natural in the target language.

            Return valid JSON only. Do not use markdown, comments, or additional text.

            JSON schema:
            {
              "MoreCorrectSentence": "A more natural corrected translation in the target language, or an empty string when no correction is needed",
              "Verdict": "Short feedback explaining the main mistake or why the answer is good",
              "IsCorrect": true
            }

            Evaluation rules:
            - Use "Sentence language" as the source sentence language.
            - Use "Translation language" as the target translation language.
            - MoreCorrectSentence must be written in the target translation language.
            - Verdict must be written in the exact language specified by "Verdict language" in the prompt.
            - Verdict must use only that one language. Do not mix languages inside Verdict, even for short grammar comments, borrowed words, or naturalness notes.
            - Bad Verdict example: "Переклад передає основний зміст, але użyto niepoprawnego czasownika; naturalніше сказати..." because it mixes Ukrainian and Polish.
            - Bad Verdict example: "Речення зрозуміле й природне. Слово użyte правильно..." because "użyte" is a Polish explanatory word inside a Ukrainian verdict.
            - Foreign words are allowed in Verdict only when they are the exact user's word or corrected target-language word being discussed, and they must be quoted.
            - Never use foreign-language grammar terms or explanatory words inside Verdict. Translate terms such as "used", "case", "verb", "natural", "correctly", and similar feedback words into the Verdict language.
            - Before returning JSON, check Verdict for mixed-language wording. If any explanatory word is not in the Verdict language, rewrite Verdict fully in the Verdict language.
            - If you need to mention a foreign word from the user's translation, keep the explanation around it in the Verdict language only.
            - Be moderately lenient: small punctuation, capitalization, word order, or article mistakes should not fail the answer if the meaning is clear and the translation is natural enough.
            - Scale leniency with sentence length: the longer the user's translation is, the more tolerant you should be of small local grammar, agreement, word choice, and style issues when the main meaning is preserved.
            - For longer translations, do not mark IsCorrect = false only because of one or two minor issues. Mark false only when the mistakes significantly damage meaning, naturalness, or target-language correctness.
            - Missing final punctuation, such as a period at the end of the sentence, must not count as a mistake, weakness, or reason to mark the answer incorrect.
            - Allow one character-level typo in the whole answer when the intended word is still clear.
            - Mark IsCorrect = true when the translation is understandable, preserves the main meaning, and is grammatically acceptable.
            - Mark IsCorrect = false when the translation changes the meaning, misses an important part, uses the wrong language, or is too unnatural/confusing.
            - IsCorrect means "good enough to pass this training item", not "perfect translation".
            - If the translation preserves the main meaning and only needs local fixes such as case, inflection, preposition, comma placement, word order polish, or a slightly more natural word form, mark IsCorrect = true.
            - You may still provide MoreCorrectSentence for a passed answer when it is genuinely more natural, but do not fail the answer for small local grammar, style, or punctuation fixes.
            - Mark IsCorrect = false only when the translation is hard to understand, changes the meaning, misses an important part, uses the wrong language, or has errors that make it unusable as a translation.
            - Do not require a literal word-for-word translation if a natural equivalent is correct.
            - Do not repeat, quote, or paraphrase MoreCorrectSentence inside Verdict.
            - Do not include labels such as "Better:", "More natural:", "Corrected:", or their translations inside Verdict. The corrected sentence belongs only in MoreCorrectSentence.
            - Keep Verdict short: one or two sentences.
            - If the user's translation is already correct, natural, and does not need a meaningful correction, set MoreCorrectSentence to an empty string.
            - Do not return MoreCorrectSentence when it would be identical or nearly identical to the user's translation.
            - Return MoreCorrectSentence only when it gives a real improvement, correction, or more natural target-language version.
            """;

        private const string QuestionInstruction = """
            You are creating one source sentence for a translation exercise.
            The user will translate this sentence into another language later.

            Return only the sentence. Do not use markdown, quotes, explanations, or additional text.

            Sentence rules:
            - Write the sentence in the exact language specified by "Sentence language" in the prompt.
            - Use the required word in a natural way.
            - You may use 1-3 additional supporting words from the provided list if they fit naturally.
            - Do not force unrelated words into the sentence.
            - Do not write meta or instructional prefixes such as "How to say:", "Translate:", or similar phrases in any language.
            - If the required word or supporting text contains such a prefix, ignore the prefix and use only the actual sentence content after it.
            - Example: from a phrase meaning "How to say: where can I find my family?", create only the sentence meaning "Where can I find my family?"
            - The sentence should be short, clear, and useful for language practice.
            - Avoid rare idioms, slang, names, and overly complex grammar.
            """;

        public async Task<TranslationSentenceResponse> CheckAnswer(TranslationSentenceRequest request, CancellationToken token)
        {
            var language = await repository.GetDeckLanguage(request.DeckId, request.UserId, token);

            string prompt = $"""
                Source sentence: {request.QuestionSentence}
                User translation: {request.AnswerSentence}
                Verdict language: {language.BackLanguage}
                Sentence language: {language.BackLanguage}
                Translation language: {language.FrontLanguage}
                """;

            var response = await transport.SendResponseAsync(prompt, AnswerInstruction, token);
            var parsedAudit = ParseAudit(response, request);

            TranslationSentenceResponse result = new TranslationSentenceResponse
            {
                MoreCorrectSentence = parsedAudit.MoreCorrectSentence,
                Verdict = parsedAudit.Verdict,
                IsCorrect = parsedAudit.IsCorrect,
            };

            return result;
        }

        public async Task<string> CreateQuestion(string[] words, string targetNote, long deckId, Guid userId, CancellationToken token)
        {
            var language = await repository.GetDeckLanguage(deckId, userId, token);

            string prompt = $"""
                Required word: {targetNote}
                Supporting words: {string.Join(", ", words)}
                Sentence language: {language.BackLanguage}
                """;

            var response = await transport.SendResponseAsync(prompt, QuestionInstruction, token);

            return response;
        }

        private GeneratedTranslationSentenceAudit ParseAudit(string response, TranslationSentenceRequest request)
        {
            try
            {
                var question = JsonSerializer.Deserialize<GeneratedTranslationSentenceAudit>(
                    response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (question is not null)
                    return question;
            }
            catch (JsonException)
            {
            }

            return new GeneratedTranslationSentenceAudit(
                request.AnswerSentence,
                "Could not parse AI verdict.",
                false);
        }

        public sealed record GeneratedTranslationSentenceAudit(string MoreCorrectSentence, string Verdict, bool IsCorrect);
    }
}
