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
        var u = await repository.Query<User>()
            .FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.AccessDenied);
        var v = request.Values;
        if (v.Count != 1) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidAdminRequest);
        var identifier = Digits(v[0].Trim().TrimStart('@'));
        var target = long.TryParse(identifier, out var telegramId)
            ? await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken)
            : await repository.Query<User>()
                .FirstOrDefaultAsync(x => x.Username != null && x.Username.ToLower() == identifier.ToLower(),
                    cancellationToken);
        if (target is null && long.TryParse(identifier, out telegramId) && telegramId > 0)
        {
            target = new User { TelegramId = telegramId };
            repository.Create(target);
        }

        if (target is null) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.AdminTargetNotFound);
        target.IsAdmin = true;
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success(global::VocabularyBot.Infrastructure.Resources.Messages.AdminAssigned);
    }
}