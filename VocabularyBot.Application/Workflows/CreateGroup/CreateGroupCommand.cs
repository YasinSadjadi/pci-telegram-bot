using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.CreateGroup;

public sealed record CreateGroupCommand(long ActorId, IReadOnlyList<string> Values) : ICommand<Result>;