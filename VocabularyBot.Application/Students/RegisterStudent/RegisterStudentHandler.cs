using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Students.RegisterStudent;
public sealed class RegisterStudentHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
 : BaseCommandHandler<RegisterStudentCommand>(loggerFactory)
{
 protected override void OnFailure() => repository.ClearChanges();
 protected override async Task<Result> Execute(RegisterStudentCommand request, CancellationToken ct)
 {
  var number = string.Concat(request.StudentNumber.Trim().Select(c => c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
  if (number.Length is < 1 or > 32 || number.Any(c => c is < '0' or > '9')) return Result.Failure("شماره دانشجویی باید فقط شامل ارقام و حداکثر ۳۲ رقم باشد.");
  var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, ct);
  if (user is null) return Result.Failure("ابتدا /start را ارسال کنید.");
  if (await repository.Query<User>().AnyAsync(x => x.StudentNumber == number && x.Id != user.Id, ct)) return Result.Failure("این شماره دانشجویی قبلاً ثبت شده است.");
  user.StudentNumber = number;
  await repository.SaveChangesAsync(ct);
  return Result.Success("✅ ثبت‌نام دانشجویی انجام شد.");
 }
}
