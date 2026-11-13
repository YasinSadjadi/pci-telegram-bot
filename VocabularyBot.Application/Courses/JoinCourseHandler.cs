using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Courses;
public sealed class JoinCourseHandler(IGenericRepository repository, ILoggerFactory loggerFactory) : BaseCommandHandler<JoinCourseCommand>(loggerFactory)
{
    protected override void OnFailure() => repository.ClearChanges();
    protected override async Task<Result> Execute(JoinCourseCommand request, CancellationToken ct)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, ct);
        if (user?.StudentNumber is null) return Result.Failure("ابتدا شماره دانشجویی خود را با /register ثبت کنید.");
        if (!Guid.TryParse(request.CourseId, out var courseId) || courseId == Guid.Empty || !await repository.Query<Course>().AnyAsync(x => x.Id == courseId, ct))
            return Result.Failure("کلاس پیدا نشد. یکی از دکمه‌های فهرست کلاس‌ها را انتخاب کنید.");
        if (await repository.Query<CourseMembership>().AnyAsync(x => x.CourseId == courseId && x.UserId == user.Id, ct))
            return Result.Success("شما از قبل عضو این کلاس هستید. برای ثبت واژه /word را بفرستید.");
        repository.Create(new CourseMembership { CourseId = courseId, UserId = user.Id });
        await repository.SaveChangesAsync(ct);
        return Result.Success("عضویت در کلاس انجام شد. برای ثبت واژه /word را بفرستید.");
    }
}
