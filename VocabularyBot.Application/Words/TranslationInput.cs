using System.ComponentModel.DataAnnotations;
using System.Reflection;
using VocabularyBot.Domain.Words;
namespace VocabularyBot.Application.Words;
public static class TranslationInput
{
    public static string Name(LanguageType type) => typeof(LanguageType).GetField(type.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? type.ToString();
    public static string SupportedLanguages => string.Join("، ", Enum.GetValues<LanguageType>().Select(Name));
    public static bool TryParse(string input, out LanguageType type, out string meaning)
    {
        type = default;
        meaning = "";
        var separator = input.IndexOfAny([':', '：']);
        if (separator < 0) return false;
        var language = Normalize(input[..separator]);
        meaning = input[(separator + 1)..].Trim();
        if (meaning.Length is < 1 or > 2000) return false;
        foreach (var candidate in Enum.GetValues<LanguageType>())
        {
            if (language == Normalize(Name(candidate)) || language == Normalize(candidate.ToString()))
            {
                type = candidate;
                return true;
            }
        }
        return false;
    }
    private static string Normalize(string value) => WordNormalizer.Normalize(value.Replace('ي', 'ی').Replace('ك', 'ک'));
}
