using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Queries.ListGroups;

public sealed record ListGroupsQuery(long ActorId) : IQuery<string>;