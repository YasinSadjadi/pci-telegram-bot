using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.AssignAdmin;

public sealed record AssignAdminCommand(long ActorId, IReadOnlyList<string> Values) : ICommand<Result>;