using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;
namespace VocabularyBot.Application.Words.ListMyWords;
public sealed class ListMyWordsHandler(IGenericRepository repository) : IQueryHandler<ListMyWordsQuery, string>
{
 public async Task<string> Handle(ListMyWordsQuery request, CancellationToken ct)
 {
  var user = await repository.Query<User>(false).FirstOrDefaultAsync(x => x.TelegramId == request.TelegramId, ct);
  if (user?.StudentNumber is null) return "ابتدا شماره دانشجویی خود را ثبت کنید: /register";
  var words = await repository.Query<VocabularyEntry>(false).Where(x => x.UserId == user.Id).OrderBy(x => x.EnglishWord).ToListAsync(ct);
  return words.Count == 0 ? "هنوز واژه‌ای ثبت نکرده‌اید." : string.Join("\n", words.Select(x => $"{x.EnglishWord} | {x.Pronunciation} | {x.PersianMeaning}"));
 }
}
