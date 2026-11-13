using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;
namespace VocabularyBot.Application.Courses;
public sealed record CreateCourseCommand(long ActorId,string University,string Year,string Term) : ICommand<Result>;
