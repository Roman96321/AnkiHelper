using Application.Abstractions;
using Application.Abstractions.OpenAI;
using Application.UseCases.Training.SentenceComposition;
using System.Text.Json;

namespace Infrastructure.OpenAI.SentenceComposition
{
    internal class SentenceCompositionCheckAnswer(OpenAITransport transport, IDeckRepository repository) : ISentenceCompositionCheckAnswer
    {
        private const string Instruction = """
            You are checking a sentence-composition language exercise.
            The user receives a list of required words and writes one sentence using them.
            Evaluate whether the user's sentence is understandable, natural enough, and uses the given words correctly.

            Return valid JSON only. Do not use markdown, comments, or additional text.

            JSON schema:
            {
              "MoreCorrectSentence": "A more natural corrected version of the user's sentence, or an empty string when no correction is needed",
              "Verdict": "Short feedback explaining the main mistake or why the answer is good",
              "IsCorrect": true
            }

            Evaluation rules:
            - Be moderately lenient: small punctuation, capitalization, or article mistakes should not fail the answer if the sentence is understandable.
            - Scale leniency with sentence length: the longer the user's sentence is, the more tolerant you should be of small local grammar, agreement, word choice, and style issues when the overall meaning is clear and the sentence is usable.
            - For longer sentences, do not mark IsCorrect = false only because of one or two minor issues. Mark false only when the mistakes significantly damage meaning, naturalness, or required-word usage.
            - Missing final punctuation, such as a period at the end of the sentence, must not count as a mistake, weakness, or reason to mark the answer incorrect.
            - Allow one character-level typo in the whole answer when the intended word is still clear.
            - Mark IsCorrect = true when the sentence is grammatically acceptable and the meaning is clear.
            - Mark IsCorrect = false when word order, grammar, or word usage makes the sentence unnatural, confusing, or wrong.
            - IsCorrect means "good enough to pass this training item", not "perfect sentence".
            - If the sentence is understandable, uses the required words, and only needs local fixes such as case, inflection, preposition, comma placement, or a slightly more natural word form, mark IsCorrect = true.
            - You may still provide MoreCorrectSentence for a passed answer when it is genuinely more natural, but do not fail the answer for small local grammar or punctuation fixes.
            - Mark IsCorrect = false only when the sentence is hard to understand, has the wrong meaning, misses an important required word, uses the wrong language, or has errors that make it unusable as a sentence.
            - Provided words can contain extra helper text such as pronunciation, transcription, translation hints, or another grammatical form after the main word.
            - Examples: "śpiące шпіонце" means the required word is "śpiące"; "Potrafić potrafię" can be satisfied by a correct sentence using a natural form such as "potrafię".
            - Do not require the user to include pronunciation, transcription, translation hints, or every grammatical form as separate words.
            - The user may use any grammatically natural form of a provided word when it is the same lemma or clearly the same target word.
            - Do not mark the answer wrong or criticize it only because the user changed a provided word form to agree with the subject, tense, case, gender, number, or sentence context.
            - Bad Verdict example: "The sentence is understandable, but the word 'śpiące' was not used in the required form" when the user's changed form is grammatically natural.
            - The corrected sentence should use only the provided words, except small function words if absolutely necessary.
            - Do not introduce new important vocabulary in MoreCorrectSentence.
            - If the user did not use an important provided word, mention that in Verdict.
            - Write Verdict in the exact language specified by "Verdict language" in the prompt.
            - Verdict must use only that one language. Do not mix languages inside Verdict, even for short grammar comments, borrowed words, or naturalness notes.
            - Bad Verdict example: "Переклад передає основний зміст, але użyto niepoprawnego czasownika; naturalніше сказати..." because it mixes Ukrainian and Polish.
            - Bad Verdict example: "Речення зрозуміле й природне. Слово użyte правильно..." because "użyte" is a Polish explanatory word inside a Ukrainian verdict.
            - Foreign words are allowed in Verdict only when they are the exact user's word or corrected target-language word being discussed, and they must be quoted.
            - Never use foreign-language grammar terms or explanatory words inside Verdict. Translate terms such as "used", "case", "verb", "natural", "correctly", and similar feedback words into the Verdict language.
            - Before returning JSON, check Verdict for mixed-language wording. If any explanatory word is not in the Verdict language, rewrite Verdict fully in the Verdict language.
            - If you need to mention a foreign word from the user's sentence, keep the explanation around it in the Verdict language only.
            - Do not repeat, quote, or paraphrase MoreCorrectSentence inside Verdict.
            - Do not include labels such as "Better:", "More natural:", "Corrected:", or their translations inside Verdict. The corrected sentence belongs only in MoreCorrectSentence.
            - Keep Verdict short: one or two sentences.
            - If the user's sentence is already correct, natural, and does not need a meaningful correction, set MoreCorrectSentence to an empty string.
            - Do not return MoreCorrectSentence when it would be identical or nearly identical to the user's sentence.
            - Return MoreCorrectSentence only when it gives a real improvement, correction, or more natural target-language version.
            """;
        public async Task<SentenceCompositionResponse> CheckAnswer(SentenceCompositionRequest request, CancellationToken token)
        {
            var language = await repository.GetDeckLanguage(request.DeckId, request.UserId, token);

            string prompt = $"""
                Provided words: {string.Join(", ", request.Words)}
                User sentence: {request.AnswerSentence}
                Verdict language: {language.BackLanguage}
                """;

            var response = await transport.SendResponseAsync(prompt, Instruction, token);
            var parsedAudit = ParseAudit(response, request);

            SentenceCompositionResponse result = new SentenceCompositionResponse 
            {
                MoreCorrectSentence = parsedAudit.MoreCorrectSentence,
                Verdict = parsedAudit.Verdict,
                IsCorrect =  parsedAudit.IsCorrect,
            };

            return result;
        }

        private GeneratedSentenceCompositionAudit ParseAudit(string response, SentenceCompositionRequest request)
        {
            try
            {
                var question = JsonSerializer.Deserialize<GeneratedSentenceCompositionAudit>(
                    response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (question is not null)
                    return question;
            }
            catch (JsonException)
            {
            }

            return new GeneratedSentenceCompositionAudit(
                request.AnswerSentence,
                "Could not parse AI verdict.",
                false);
        }

        public sealed record GeneratedSentenceCompositionAudit(string MoreCorrectSentence, string Verdict, bool IsCorrect);
    }
}
