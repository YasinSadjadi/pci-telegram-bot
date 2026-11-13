using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;
namespace VocabularyBot.Application.Students.RegisterStudent;
public sealed record RegisterStudentCommand(long TelegramId, string StudentNumber) : ICommand<Result>;
