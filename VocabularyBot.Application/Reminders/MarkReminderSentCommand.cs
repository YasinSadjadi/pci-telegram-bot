using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Reminders;

public sealed record MarkReminderSentCommand(Guid WeekId, Guid GroupId, Guid UserId, DateTime SentUtc) : ICommand<bool>;