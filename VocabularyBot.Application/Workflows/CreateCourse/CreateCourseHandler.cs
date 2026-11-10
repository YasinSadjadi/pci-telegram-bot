using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows.CreateCourse;

public sealed class CreateCourseHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : WorkflowHandlerBase<CreateCourseCommand>(repository, loggerFactory)
{
    protected override async Task<Result> Execute(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 3) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        repository.Create(new Course { University = v[0], Year = Num(v[1]), Term = Num(v[2]) });
        await repository.SaveChangesAsync();
        return Result.Success("کلاس ایجاد شد. شناسه را با /classes ببینید.");
    }
}