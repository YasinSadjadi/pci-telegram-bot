using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.ListUsers;

public sealed class ListUsersHandler(IGenericRepository repository) : IQueryHandler<ListUsersQuery, string>
{
    public async Task<string> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return "ابتدا /start را ارسال کنید.";
        if (!user.IsAdmin) return "دسترسی ندارید.";
        var us = await repository.Query<User>().OrderBy(x => x.Id).Take(100).ToListAsync();
        return string.Join('\n',
            us.Select(x => $"{x.TelegramId} | @{x.Username ?? "-"} | {x.Name} | {(x.IsAdmin ? "ادمین" : "عضو")}"));
    }
}