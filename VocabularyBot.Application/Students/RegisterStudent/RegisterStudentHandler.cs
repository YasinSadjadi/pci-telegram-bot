using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;
using VocabularyBot.Infrastructure.Resources;

namespace VocabularyBot.Application.Students.RegisterStudent;

public sealed class RegisterStudentHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : BaseCommandHandler<RegisterStudentCommand>(loggerFactory)
{
    protected override void OnFailure()
    {
        repository.ClearChanges();
    }

    protected override async Task<Result> Execute(RegisterStudentCommand request, CancellationToken ct)
    {
        var number = Digits.ToAscii(request.StudentNumber.Trim());
        if (number.Length is < 1 or > 32 || number.Any(c => c is < '0' or > '9'))
            return Result.Failure(Error.InvalidStudentNumber);
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, ct);
        if (user is null) return Result.Failure(Error.StartRequired);
        if (await repository.Query<User>().AnyAsync(x => x.StudentNumber == number && x.Id != user.Id, ct))
            return Result.Failure(Error.DuplicateStudentNumber);
        user.StudentNumber = number;
        await repository.SaveChangesAsync(ct);
        return Result.Success(Messages.StudentRegistered);
    }
}
