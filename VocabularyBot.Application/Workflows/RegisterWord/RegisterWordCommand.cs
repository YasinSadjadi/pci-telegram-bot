using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.RegisterWord;

public sealed record RegisterWordCommand(long ActorId, IReadOnlyList<string> Values) : ICommand<Result>;