using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
namespace VocabularyBot.Application.Courses;
public sealed record ListCoursesQuery(long ActorId) : IQuery<string>;
