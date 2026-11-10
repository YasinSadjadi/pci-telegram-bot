using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Queries.ListWeeks;

public sealed record ListWeeksQuery(long ActorId) : IQuery<string>;