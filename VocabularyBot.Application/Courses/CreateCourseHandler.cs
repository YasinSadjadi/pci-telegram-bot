using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Courses;

public sealed class CreateCourseHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : BaseCommandHandler<CreateCourseCommand>(loggerFactory)
{
    protected override void OnFailure()
    {
        repository.ClearChanges();
    }

    protected override async Task<Result> Execute(CreateCourseCommand request, CancellationToken ct)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, ct);
        if (user?.IsAdmin != true) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.CourseAdminRequired);
        if (string.IsNullOrWhiteSpace(request.University) || request.University.Length > 200)
            return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidUniversityName);
        if (!int.TryParse(Digits.ToAscii(request.Year), out var year) || year <= 0 ||
            !int.TryParse(Digits.ToAscii(request.Term), out var term) ||
            term <= 0) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidCourseTerm);
        var course = new Course { University = request.University.Trim(), Year = year, Term = term };
        repository.Create(course);
        await repository.SaveChangesAsync(ct);
        return Result.Success(global::VocabularyBot.Infrastructure.Resources.Messages.CourseCreated);
    }
}
