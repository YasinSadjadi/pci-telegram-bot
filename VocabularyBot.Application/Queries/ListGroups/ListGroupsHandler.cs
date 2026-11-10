using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.ListGroups;

public sealed class ListGroupsHandler(IGenericRepository repository) : IQueryHandler<ListGroupsQuery, string>
{
    public async Task<string> Handle(ListGroupsQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return "ابتدا /start را ارسال کنید.";
        var gs = await repository.Query<StudyGroup>()
            .Where(g => user.IsAdmin || repository.Query<Membership>()
                .Any(m => m.GroupId == g.Id && m.UserId == user.Id)).OrderBy(x => x.Id).ToListAsync();
        return gs.Count == 0
            ? "گروهی یافت نشد."
            : string.Join('\n', gs.Select(x => $"شناسه {x.Id} | کلاس {x.CourseId} | گروه {x.Number}"));
    }
}