using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Courses;
public sealed class CreateCourseHandler(IGenericRepository repository,ILoggerFactory loggerFactory) : BaseCommandHandler<CreateCourseCommand>(loggerFactory)
{
 protected override void OnFailure() => repository.ClearChanges();
 protected override async Task<Result> Execute(CreateCourseCommand request,CancellationToken ct)
 {
  var user=await repository.Query<User>().FirstOrDefaultAsync(x=>x.TelegramId==request.ActorId,ct);
  if (user?.IsAdmin != true) return Result.Failure("فقط ادمین می‌تواند کلاس بسازد.");
  if (string.IsNullOrWhiteSpace(request.University) || request.University.Length>200) return Result.Failure("نام دانشگاه را وارد کنید؛ حداکثر ۲۰۰ نویسه مجاز است.");
  if (!int.TryParse(Digits.Convert(request.Year),out int year) || year<=0 || !int.TryParse(Digits.Convert(request.Term),out int term) || term<=0) return Result.Failure("سال و شماره ترم باید عدد مثبت باشند. لطفاً دوباره وارد کنید.");
  var course=new Course{University=request.University.Trim(),Year=year,Term=term};repository.Create(course);await repository.SaveChangesAsync(ct);
  return Result.Success($"کلاس ساخته شد. شناسه کلاس: {course.Id}");
 }
}
