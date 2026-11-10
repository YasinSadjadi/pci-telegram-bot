using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Reminders;

public sealed class GetDueRemindersHandler(IGenericRepository repository)
    : IQueryHandler<GetDueRemindersQuery, List<DueReminder>>
{
    public Task<List<DueReminder>> Handle(GetDueRemindersQuery request, CancellationToken cancellationToken)
    {
        return (from w in repository.Query<StudyWeek>()
                join g in repository.Query<StudyGroup>() on w.CourseId equals g.CourseId
                join m in repository.Query<Membership>() on g.Id equals m.GroupId
                join u in repository.Query<User>() on m.UserId equals u.Id
                where w.ReminderUtc <= request.Now && !repository.Query<Reminder>()
                    .Any(r => r.WeekId == w.Id && r.GroupId == g.Id && r.UserId == m.UserId)
                select new DueReminder(w.Id, w.Label, g.Id, m.UserId, u.TelegramId,
                    repository.Query<Word>().Count(x => x.WeekId == w.Id && x.GroupId == g.Id && x.UserId == m.UserId)))
            .ToListAsync(cancellationToken);
    }
}