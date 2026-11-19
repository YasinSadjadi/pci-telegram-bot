using System.Text;

namespace VocabularyBot.Application;

internal static class Digits
{
    public static string ToAscii(string value)
    {
        var normalized = new StringBuilder(value.Length);
        var index = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsDigit(rune))
                normalized.Append((char)('0' + (int)Rune.GetNumericValue(rune)));
            else
                normalized.Append(value, index, rune.Utf16SequenceLength);

            index += rune.Utf16SequenceLength;
        }

        return normalized.ToString();
    }
}
