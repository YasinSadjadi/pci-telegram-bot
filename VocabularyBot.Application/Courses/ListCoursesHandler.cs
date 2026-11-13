using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Courses;
public sealed class ListCoursesHandler(IGenericRepository repository) : IQueryHandler<ListCoursesQuery,string>
{
 public async Task<string> Handle(ListCoursesQuery request,CancellationToken ct)
 {
  var user=await repository.Query<User>().FirstOrDefaultAsync(x=>x.TelegramId==request.ActorId,ct);
  if(user is null) return "ابتدا /start را ارسال کنید.";
  var query=repository.Query<Course>(false);
  if(!user.IsAdmin) query=query.Where(c=>repository.Query<CourseMembership>(false).Any(m=>m.CourseId==c.Id&&m.UserId==user.Id));
  var courses=await query.OrderBy(c=>c.University).ThenBy(c=>c.Year).ThenBy(c=>c.Term).ToListAsync(ct);
  return courses.Count==0?"کلاسی برای نمایش وجود ندارد. برای عضویت با ادمین هماهنگ کنید.":string.Join("\n",courses.Select(c=>$"{c.Id} | {c.University} | {c.Year}-{c.Term}"));
 }
}
