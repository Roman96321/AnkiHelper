using System.Net;
using System.Text.RegularExpressions;

namespace Application.UseCases.Decks.DeckImports;

internal static class AnkiFieldTextCleaner
{
    private static readonly Regex SoundTagRegex = new(@"\[sound:[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Clean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var withoutSoundTags = SoundTagRegex.Replace(value, " ");
        var withoutHtmlTags = HtmlTagRegex.Replace(withoutSoundTags, " ");
        var decoded = WebUtility.HtmlDecode(withoutHtmlTags).Replace('\u00A0', ' ');

        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }
}
