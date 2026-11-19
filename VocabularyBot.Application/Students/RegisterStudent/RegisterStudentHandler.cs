using System.Text.RegularExpressions;
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
        var number = NormalizeNumber(request.StudentNumber);
        if (number is null)
            return Result.Failure(Error.InvalidStudentNumber);
        var fullName = Regex.Replace(request.FullName.Trim(), @"[^\S\r\n]+", " ");
        if (fullName.Length > 100 || !Regex.IsMatch(fullName,
                @"\A\p{L}[\p{L}\p{M}\u200C'’\-]*(?: \p{L}[\p{L}\p{M}\u200C'’\-]*)+\z"))
            return Result.Failure(Messages.InvalidFullName);
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, ct);
        if (user is null) return Result.Failure(Error.StartRequired);
        if (await repository.Query<User>().AnyAsync(x => x.StudentNumber == number && x.Id != user.Id, ct))
            return Result.Failure(Error.DuplicateStudentNumber);
        user.StudentNumber = number;
        user.FullName = fullName;
        await repository.SaveChangesAsync(ct);
        return Result.Success(Messages.StudentRegistered);
    }
    internal static string? NormalizeNumber(string input)
    {
        var number = Digits.ToAscii(input.Trim());
        return number.Length is >= 1 and <= 32 && number.All(c => c is >= '0' and <= '9')
            ? number
            : null;
    }
}
