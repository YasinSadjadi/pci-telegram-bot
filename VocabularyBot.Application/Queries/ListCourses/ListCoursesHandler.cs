using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Queries.ListCourses;

public sealed class ListCoursesHandler(IGenericRepository repository) : IQueryHandler<ListCoursesQuery, string>
{
    public async Task<string> Handle(ListCoursesQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (user is null) return "ابتدا /start را ارسال کنید.";
        var cs = await repository.Query<Course>().OrderBy(x => x.Id).ToListAsync();
        return cs.Count == 0
            ? "کلاسی ثبت نشده است."
            : string.Join('\n', cs.Select(x => $"{x.Id}: {x.University} | {x.Year}-{x.Term}"));
    }
}