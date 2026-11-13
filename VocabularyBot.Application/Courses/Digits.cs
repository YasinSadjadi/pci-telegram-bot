namespace VocabularyBot.Application.Courses;
internal static class Digits
{
 public static string Convert(string s)=>string.Concat(s.Select(c=>c is >= '۰' and <= '۹'?(char)('0'+c-'۰'):c is >= '٠' and <= '٩'?(char)('0'+c-'٠'):c));
}
