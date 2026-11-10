using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Queries.ListUsers;

public sealed record ListUsersQuery(long ActorId) : IQuery<string>;