using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.ListUsers;

public sealed class ListUsersHandler(IGenericRepository repository) : IQueryHandler<ListUsersQuery, string>
{
    public async Task<string> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>()
            .FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return global::VocabularyBot.Infrastructure.Resources.Error.StartRequired;
        if (!user.IsAdmin) return global::VocabularyBot.Infrastructure.Resources.Error.AccessDenied;
        var us = await repository.Query<User>().OrderBy(x => x.Id).Take(100).ToListAsync();
        return string.Join('\n',
            us.Select(x => $"{x.TelegramId} | @{x.Username ?? "-"} | {x.Name} | {(x.IsAdmin ? global::VocabularyBot.Infrastructure.Resources.Messages.UserRoleAdmin : global::VocabularyBot.Infrastructure.Resources.Messages.UserRoleMember)}"));
    }
}