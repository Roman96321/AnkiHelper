using Application.Abstractions.OpenAI;

namespace Infrastructure.OpenAI
{
    internal class DetermineLanguage(OpenAITransport transport) : IDetermineLanguage
    {
        private const string Instruction = """
            Detect the language of the meaningful human words in the input.
            Ignore HTML, media tags, filenames, punctuation, examples, and technical noise.
            Return exactly one code only: uk, en, pl, ru, de, es, fr, or und.
            Use ISO 639-1 codes only. Normalize names, regional codes, and 3-letter codes to that list.
            Return und if unsure. No JSON, quotes, punctuation, or explanation.
            """;

        public async Task<string> Determine(string[] words, CancellationToken token)
        {
            if (words.Length == 0)
                throw new ArgumentException("At least one word is required to determine language.", nameof(words));

            string prompt = string.Join(", ", words);

            var res = await transport.SendResponseAsync(prompt, Instruction, token);

            return ToLanguageName(NormalizeLanguageCode(res));
        }

        private static string NormalizeLanguageCode(string response)
        {
            var languageCode = response
                .Trim()
                .ToLowerInvariant()
                .Trim('"', '\'', '`', '.', ',', ';', ':', ' ');

            return languageCode switch
            {
                "uk" or "ua" or "ukr" or "uk-ua" or "uk_ua" or "ukrainian" => "uk",
                "en" or "eng" or "en-us" or "en-gb" or "english" => "en",
                "pl" or "pol" or "pl-pl" or "pl-pol" or "polish" => "pl",
                "ru" or "rus" or "ru-ru" or "russian" => "ru",
                "de" or "deu" or "ger" or "de-de" or "german" => "de",
                "es" or "spa" or "es-es" or "spanish" => "es",
                "fr" or "fra" or "fre" or "fr-fr" or "french" => "fr",
                "und" or "unknown" or "undefined" => "und",
                _ => languageCode
            };
        }

        private static string ToLanguageName(string languageCode)
        {
            return languageCode switch
            {
                "uk" => "Ukrainian",
                "en" => "English",
                "pl" => "Polish",
                "ru" => "Russian",
                "de" => "German",
                "es" => "Spanish",
                "fr" => "French",
                "und" => "unknown",
                _ => languageCode
            };
        }

    }
}
