using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.GetStatus;

public sealed class GetStatusHandler(IGenericRepository repository) : IQueryHandler<GetStatusQuery, string>
{
    public async Task<string> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return "ابتدا /start را ارسال کنید.";
        if (!user.IsAdmin) return "دسترسی ندارید.";
        var data = await (from m in repository.Query<Membership>()
                          join g in repository.Query<StudyGroup>() on m.GroupId equals g.Id
                          join w in repository.Query<StudyWeek>() on g.CourseId equals w.CourseId
                          join u in repository.Query<User>() on m.UserId equals u.Id
                          orderby w.Id descending
                          select new
                          {
                              g.CourseId,
                              GroupNumber = g.Number,
                              WeekLabel = w.Label,
                              UserName = u.Name,
                              UserId = u.TelegramId,
                              Count = repository.Query<Word>().Count(x => x.WeekId == w.Id && x.GroupId == g.Id && x.UserId == u.Id)
                          }).Take(100).ToListAsync();
        return data.Count == 0
            ? "گزارشی وجود ندارد."
            : string.Join('\n',
                data.Select(x =>
                    $"کلاس {x.CourseId}، گروه {x.GroupNumber}، {x.WeekLabel}، {x.UserName} ({x.UserId}): {x.Count}/5"));
    }
}