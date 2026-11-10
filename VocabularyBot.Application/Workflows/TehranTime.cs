namespace VocabularyBot.Application.Workflows;

public static class TehranTime
{
    public static DateTime Local(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
    }
}