namespace VocabularyBot.Application.Reminders;

public sealed record DueReminder(Guid WeekId, string WeekLabel, Guid GroupId, Guid UserId, long TelegramId, int Count);