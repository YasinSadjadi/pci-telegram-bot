using System.Text;
using System.Text.RegularExpressions;

namespace VocabularyBot.Domain.Words;

public static partial class WordNormalizer
{
    public static string Normalize(string value)
    {
        return Whitespace().Replace(value.Normalize(NormalizationForm.FormKC).Trim(), " ").ToUpperInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}