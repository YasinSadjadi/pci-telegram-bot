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
        if (user?.StudentNumber is null) return global::VocabularyBot.Infrastructure.Resources.Error.StudentRegistrationPromptRequired;
        var words = await repository.Query<VocabularyEntry>(false).Where(x => x.UserId == user.Id)
            .OrderBy(x => x.EnglishWord).ToListAsync(ct);
        if (words.Count == 0)
            return
                global::VocabularyBot.Infrastructure.Resources.Messages.EmptyWordList;
        var ids = words.Select(x => x.Id).ToArray();
        var translations = await repository.Query<WordTranslation>(false).Where(x => ids.Contains(x.VocabularyEntryId))
            .OrderBy(x => x.Type).ToListAsync(ct);
        var byWord = translations.ToLookup(x => x.VocabularyEntryId);
        return string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.MyWordsHeading, words.Count) + string.Join(global::VocabularyBot.Infrastructure.Resources.Messages.WordListSeparator,
            words.Select(word => Format(word, byWord[word.Id])));
    }

    internal static string Format(VocabularyEntry word, IEnumerable<WordTranslation> translations)
    {
        var lines = new List<string>
            { string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.WordHeading, word.EnglishWord), string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.PronunciationLabel, word.Pronunciation), string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.PersianMeaningLabel, word.PersianMeaning) };
        Add(lines, global::VocabularyBot.Infrastructure.Resources.Messages.WordFamilyLabel, word.Family);
        Add(lines, global::VocabularyBot.Infrastructure.Resources.Messages.SynonymsLabel, word.Synonyms);
        foreach (var translation in translations)
            Add(lines, TranslationInput.Name(translation.Type), translation.Meaning);
        return string.Join("\n", lines);
    }

    private static void Add(List<string> lines, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) lines.Add($"{label}: {value}");
    }
}