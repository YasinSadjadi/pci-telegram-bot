using System.Text.RegularExpressions;
using VocabularyBot.Domain.Words;
namespace VocabularyBot.Application.Words;
public static class TranslationInput
{
    public static string NormalizeLanguage(string language) => WordNormalizer.Normalize(language.Replace('ي', 'ی').Replace('ك', 'ک'));

    public static bool TryParse(string input, out string language, out string meaning)
    {
        language = meaning = "";
        var separator = input.IndexOfAny([':', '：']);
        if (separator < 0) return false;
        language = input[..separator].Trim();
        meaning = input[(separator + 1)..].Trim();
        return language.Length is > 0 and <= 100 && meaning.Length is > 0 and <= 2000 && !language.Contains('\n') && !language.Contains('\r');
    }
}
