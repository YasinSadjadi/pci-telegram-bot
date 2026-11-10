using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Queries.GetStatus;

public sealed record GetStatusQuery(long ActorId) : IQuery<string>;