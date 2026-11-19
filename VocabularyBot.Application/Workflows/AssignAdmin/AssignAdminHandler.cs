using System.Text.RegularExpressions;
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
        var identifier = v[0].Trim();
        if (!identifier.StartsWith('@') && long.TryParse(Digits(identifier), out var telegramId))
        {
            if (telegramId <= 0) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidAdminRequest);
            var target = await repository.Query<User>()
                .FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken);
            if (target is null)
            {
                target = new User { TelegramId = telegramId };
                repository.Create(target);
            }

            target.IsAdmin = true;
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(global::VocabularyBot.Infrastructure.Resources.Messages.AdminAssigned);
        }

        if (!Regex.IsMatch(identifier, @"\A@?[A-Za-z0-9_]{1,32}\z") ||
            !Regex.IsMatch(identifier, "[A-Za-z]"))
            return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidAdminRequest);

        // Verify ownership against the username from a fresh Telegram update.
        var username = identifier.TrimStart('@').ToLowerInvariant();
        if (!await repository.Query<PendingAdmin>().AnyAsync(x => x.Username == username, cancellationToken))
        {
            repository.Create(new PendingAdmin { Username = username });
            await repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(global::VocabularyBot.Infrastructure.Resources.Messages.AdminPending);
    }
}
