using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Application.Workflows.CreateCourse;

public sealed record CreateCourseCommand(long ActorId, IReadOnlyList<string> Values) : ICommand<Result>;