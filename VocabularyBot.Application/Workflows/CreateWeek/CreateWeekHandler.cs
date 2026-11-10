using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows.CreateWeek;

public sealed class CreateWeekHandler(IGenericRepository repository, ILoggerFactory loggerFactory)
    : WorkflowHandlerBase<CreateWeekCommand>(repository, loggerFactory)
{
    protected override async Task<Result> Execute(CreateWeekCommand request, CancellationToken cancellationToken)
    {
        var u = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == request.ActorId, cancellationToken);
        if (u is null || !u.IsAdmin) return Result.Failure("دسترسی ندارید.");
        var v = request.Values;
        if (v.Count != 5) return Result.Failure("اطلاعات درخواست معتبر نیست.");
        var course = ParseId(v[0]);
        if (!await repository.Query<Course>().AnyAsync(x => x.Id == course))
            throw new InvalidOperationException("کلاس وجود ندارد.");
        var start = ParseLocal(v[2]);
        var end = ParseLocal(v[3]);
        if (end <= start) throw new InvalidOperationException("پایان باید بعد از شروع باشد.");
        if (!TimeOnly.TryParseExact(Digits(v[4]), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var t)) throw new FormatException("ساعت نامعتبر است.");
        var last = Local(end).Date.Add(t.ToTimeSpan());
        var reminder =
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(last, DateTimeKind.Unspecified), Tehran);
        if (reminder < start || reminder > end)
            throw new InvalidOperationException("یادآوری باید در بازه هفته باشد.");
        if (await repository.Query<StudyWeek>()
                .AnyAsync(x => x.CourseId == course && x.StartUtc < end && x.EndUtc > start))
            throw new InvalidOperationException("هفته‌ها نباید هم‌پوشانی داشته باشند.");
        repository.Create(new StudyWeek
        { CourseId = course, Label = v[1], StartUtc = start, EndUtc = end, ReminderUtc = reminder });
        await repository.SaveChangesAsync();
        return Result.Success("هفته ثبت شد.");
    }
}