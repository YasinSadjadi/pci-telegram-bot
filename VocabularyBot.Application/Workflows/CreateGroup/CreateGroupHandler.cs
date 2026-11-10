using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows.CreateGroup;

public sealed class CreateGroupHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : WorkflowHandlerBase<CreateGroupCommand>(repository, loggerFactory)
{
    protected override async Task<Result> Execute(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 2) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        var cid = ParseId(v[0]);
        if (!await repository.Query<Course>().AnyAsync(x => x.Id == cid))
            throw new InvalidOperationException("کلاس وجود ندارد.");
        repository.Create(new StudyGroup { CourseId = cid, Number = Num(v[1]) });
        await repository.SaveChangesAsync();
        return Result.Success("گروه ایجاد شد. شناسه را با /groups ببینید.");
    }
}