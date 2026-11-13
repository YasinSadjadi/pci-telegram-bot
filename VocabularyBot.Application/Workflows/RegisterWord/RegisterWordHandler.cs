using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Domain.Words;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Workflows.RegisterWord;
public sealed class RegisterWordHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
 : WorkflowHandlerBase<RegisterWordCommand>(repository, loggerFactory)
{
 protected override async Task<Result> Execute(RegisterWordCommand request, CancellationToken ct)
 {
  var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, ct);
  if (user?.StudentNumber is null) return Result.Failure("ابتدا شماره دانشجویی خود را ثبت کنید: /register");
  var v = request.Values;
  if (v.Count != 7 || v.Take(4).Any(string.IsNullOrWhiteSpace)) return Result.Failure("کلمه انگلیسی، تلفظ و معنی فارسی اجباری هستند.");
  if (!Guid.TryParse(v[0], out var courseId) || !await repository.Query<CourseMembership>().AnyAsync(x => x.UserId == user.Id && x.CourseId == courseId, ct)) return Result.Failure("شناسه کلاس معتبر نیست یا شما عضو آن نیستید. /classes");
  var normalized = WordNormalizer.Normalize(v[1]);
  if (await repository.Query<VocabularyEntry>().AnyAsync(x => x.NormalizedWord == normalized, ct) ||
      await repository.Query<Word>().AnyAsync(x => x.NormalizedTitle == normalized, ct)) return Result.Failure("این واژه قبلاً ثبت شده است.");
  repository.Create(new VocabularyEntry { UserId = user.Id, CourseId = courseId, EnglishWord = v[1].Trim(), NormalizedWord = normalized, Pronunciation = v[2].Trim(), PersianMeaning = v[3].Trim(), Family = Optional(v[4]), Synonyms = Optional(v[5]), Translations = Optional(v[6]) });
  await repository.SaveChangesAsync(ct);
  return Result.Success("واژه ثبت شد.");
 }
 private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
