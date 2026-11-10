using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Queries.ListCourses;

public sealed record ListCoursesQuery(long ActorId) : IQuery<string>;