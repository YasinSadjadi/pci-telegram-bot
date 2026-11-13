using VocabularyBot.Application.Words;
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
        if (words.Count == 0) return "📚 واژه‌های من\n\nهنوز واژه‌ای ثبت نکرده‌اید.\nبا «ثبت واژه» یا /word اولین واژه‌تان را اضافه کنید.";
        var ids = words.Select(x => x.Id).ToArray();
        var translations = await repository.Query<WordTranslation>(false).Where(x => ids.Contains(x.VocabularyEntryId)).OrderBy(x => x.Type).ToListAsync(ct);
        var byWord = translations.ToLookup(x => x.VocabularyEntryId);
        return $"📚 واژه‌های من · {words.Count} واژه\n\n" + string.Join("\n\n────────────\n\n", words.Select(word => Format(word, byWord[word.Id])));
    }

    private static string Format(VocabularyEntry word, IEnumerable<WordTranslation> translations)
    {
        var lines = new List<string> { $"📌 {word.EnglishWord}", $"تلفظ: {word.Pronunciation}", $"معنی فارسی: {word.PersianMeaning}" };
        Add(lines, "خانواده واژه", word.Family);
        Add(lines, "مترادف", word.Synonyms);
        foreach (var translation in translations) Add(lines, TranslationInput.Name(translation.Type), translation.Meaning);
        return string.Join("\n", lines);
    }

    private static void Add(List<string> lines, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) lines.Add($"{label}: {value}");
    }
}
