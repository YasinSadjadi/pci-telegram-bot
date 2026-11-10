using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows.AssignAdmin;

public sealed class AssignAdminHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : WorkflowHandlerBase<AssignAdminCommand>(repository, loggerFactory)
{
    protected override async Task<Result> Execute(AssignAdminCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 1) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        var target = await FindUser(v[0]);
        target.IsAdmin = true;
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success("نقش ادمین ثبت شد.");
    }
}