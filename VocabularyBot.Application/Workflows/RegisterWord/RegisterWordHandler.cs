using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Application.Words;
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
        if (user?.StudentNumber is null) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.StudentRegistrationPromptRequired);
        var v = request.Values;
        if (v.Count < 6 || v.Take(4).Any(string.IsNullOrWhiteSpace))
            return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.RequiredWordDetailsMissing);
        if (!Guid.TryParse(v[0], out var courseId) || !await repository.Query<CourseMembership>()
                .AnyAsync(x => x.UserId == user.Id && x.CourseId == courseId, ct))
            return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidWordCourseMembership);
        var normalized = WordNormalizer.Normalize(v[1]);
        if (await repository.Query<VocabularyEntry>().AnyAsync(x => x.NormalizedWord == normalized, ct))
            return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.DuplicateWord);
        var translations = new List<WordTranslation>();
        var languages = new HashSet<LanguageType>();
        foreach (var line in v.Skip(6))
        {
            if (!TranslationInput.TryParse(line, out var type, out var meaning))
                return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.InvalidTranslationFormat);
            if (!languages.Add(type)) return Result.Failure(string.Format(global::VocabularyBot.Infrastructure.Resources.Error.DuplicateTranslationLanguage, TranslationInput.Name(type)));
            translations.Add(new WordTranslation { Type = type, Meaning = meaning });
        }

        var entry = new VocabularyEntry
        {
            UserId = user.Id, CourseId = courseId, EnglishWord = v[1].Trim(), NormalizedWord = normalized,
            Pronunciation = v[2].Trim(), PersianMeaning = v[3].Trim(), Family = v[4].Trim(), Synonyms = Optional(v[5])
        };
        repository.Create(entry);
        foreach (var translation in translations)
        {
            translation.VocabularyEntryId = entry.Id;
            repository.Create(translation);
        }

        await repository.SaveChangesAsync(ct);
        return Result.Success(global::VocabularyBot.Infrastructure.Resources.Messages.WordRegistered);
    }

    private static string? Optional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}