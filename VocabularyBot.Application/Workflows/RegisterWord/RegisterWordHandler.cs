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
    protected override async Task<Result> Execute(RegisterWordCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 8) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        var gid = ParseId(v[0]);
        var g = await repository.FindAsync<StudyGroup>(gid) ??
                throw new InvalidOperationException("گروه وجود ندارد.");
        if (!await repository.Query<Membership>().AnyAsync(x => x.GroupId == gid && x.UserId == u.Id))
            throw new InvalidOperationException("عضو این گروه نیستید.");
        var week = await repository.FindAsync<StudyWeek>(ParseId(v[1])) ??
                   throw new InvalidOperationException("هفته وجود ندارد.");
        var now = DateTime.UtcNow;
        if (week.CourseId != g.CourseId || now < week.StartUtc || now >= week.EndUtc)
            throw new InvalidOperationException("هفته برای این کلاس فعال نیست.");
        var normalized = WordNormalizer.Normalize(v[2]);
        if (await repository.Query<Word>().AnyAsync(x => x.CourseId == g.CourseId && x.NormalizedTitle == normalized))
            throw new InvalidOperationException("این واژه قبلاً در کلاس ثبت شده است.");
        repository.Create(new Word
        {
            CourseId = g.CourseId,
            GroupId = gid,
            WeekId = week.Id,
            UserId = u.Id,
            Title = v[2],
            NormalizedTitle = normalized,
            Meaning = v[3],
            Definition = v[4],
            Example = v[5],
            Related = v[6],
            Source = v[7]
        });
        await repository.SaveChangesAsync();
        var count = await repository.Query<Word>()
            .CountAsync(x => x.WeekId == week.Id && x.GroupId == gid && x.UserId == u.Id);
        return Result.Success($"واژه ثبت شد. تعداد این هفته: {count}/5");
    }
}