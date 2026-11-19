using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Courses;

public sealed class ListCoursesHandler(IGenericRepository repository) : IQueryHandler<ListCoursesQuery, string>
{
    public async Task<string> Handle(ListCoursesQuery request, CancellationToken ct)
    {
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, ct);
        if (user is null) return global::VocabularyBot.Infrastructure.Resources.Error.StartRequired;
        var query = repository.Query<Course>(false);
        if (!user.IsAdmin)
            query = query.Where(c =>
                repository.Query<CourseMembership>(false).Any(m => m.CourseId == c.Id && m.UserId == user.Id));
        var courses = await query.OrderBy(c => c.University).ThenBy(c => c.Year).ThenBy(c => c.Term).ToListAsync(ct);
        if (courses.Count == 0)
            return
                global::VocabularyBot.Infrastructure.Resources.Messages.EmptyCoursesList;
        var title = user.IsAdmin ? global::VocabularyBot.Infrastructure.Resources.Messages.AllCoursesTitle : global::VocabularyBot.Infrastructure.Resources.Messages.MyCoursesTitle;
        return string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.CoursesListHeading, title, courses.Count) + string.Join("\n\n",
            courses.Select(c => string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.CourseListItem, c.University, c.Year, c.Term)));
    }
}