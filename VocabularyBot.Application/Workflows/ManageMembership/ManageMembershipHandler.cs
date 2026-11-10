using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows.ManageMembership;

public sealed class ManageMembershipHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : WorkflowHandlerBase<ManageMembershipCommand>(repository, loggerFactory)
{
    protected override async Task<Result> Execute(ManageMembershipCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 2) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        var group = await repository.FindAsync<StudyGroup>(ParseId(v[0])) ??
                    throw new InvalidOperationException("گروه وجود ندارد.");
        var member = await FindUser(v[1]);
        var exists = await repository.Query<Membership>().AnyAsync(x => x.GroupId == group.Id && x.UserId == member.Id);
        if (!request.IsLeader)
        {
            if (exists) throw new InvalidOperationException("کاربر عضو گروه است.");
            repository.Create(new Membership { GroupId = group.Id, UserId = member.Id });
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success("عضو اضافه شد.");
        }

        if (!exists) throw new InvalidOperationException("کاربر عضو این گروه نیست.");
        group.LeaderId = member.Id;
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success("سرگروه تعیین شد.");
    }
}