using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Reminders;

public sealed record GetDueRemindersQuery(DateTime Now) : IQuery<List<DueReminder>>;