using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.CreateWeek;

public sealed record CreateWeekCommand(long ActorId, IReadOnlyList<string> Values) : ICommand<Result>;