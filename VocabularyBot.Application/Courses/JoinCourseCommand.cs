using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;
namespace VocabularyBot.Application.Courses;
public sealed record JoinCourseCommand(long TelegramId, string CourseId) : ICommand<Result>;
