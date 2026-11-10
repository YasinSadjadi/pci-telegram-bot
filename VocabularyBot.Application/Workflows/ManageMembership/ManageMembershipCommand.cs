using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.ManageMembership;

public sealed record ManageMembershipCommand(long ActorId, IReadOnlyList<string> Values, bool IsLeader)
    : ICommand<Result>;