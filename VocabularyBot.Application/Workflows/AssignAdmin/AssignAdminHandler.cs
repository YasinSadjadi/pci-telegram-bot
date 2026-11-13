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
        var identifier = Digits(v[0].Trim().TrimStart('@'));
        var target = long.TryParse(identifier, out var telegramId)
            ? await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken)
            : await repository.Query<User>().FirstOrDefaultAsync(x => x.Username != null && x.Username.ToLower() == identifier.ToLower(), cancellationToken);
        if (target is null) return Result.Failure("کاربر پیدا نشد. ابتدا باید /start را ارسال کند؛ شناسه را بررسی کنید.");
        target.IsAdmin = true;
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success("نقش ادمین ثبت شد.");
    }
}