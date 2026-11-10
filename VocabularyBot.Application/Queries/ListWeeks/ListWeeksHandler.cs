using Microsoft.EntityFrameworkCore;
using VocabularyBot.Application.Workflows;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.ListWeeks;

public sealed class ListWeeksHandler(IGenericRepository repository) : IQueryHandler<ListWeeksQuery, string>
{
    public async Task<string> Handle(ListWeeksQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return "ابتدا /start را ارسال کنید.";
        var ws = await repository.Query<StudyWeek>().Where(w => user.IsAdmin || repository.Query<StudyGroup>().Any(g =>
            g.CourseId == w.CourseId && repository.Query<Membership>()
                .Any(m => m.GroupId == g.Id && m.UserId == user.Id))).OrderBy(x => x.Id).ToListAsync();
        return ws.Count == 0
            ? "هفته‌ای یافت نشد."
            : string.Join('\n',
                ws.Select(x =>
                    $"شناسه {x.Id} | کلاس {x.CourseId} | {x.Label} | {TehranTime.Local(x.StartUtc):yyyy-MM-dd HH:mm} تا {TehranTime.Local(x.EndUtc):yyyy-MM-dd HH:mm}"));
    }
}