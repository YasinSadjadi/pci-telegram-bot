using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Workflows;

public abstract class WorkflowHandlerBase<TCommand>(IGenericRepository repository, ILoggerFactory loggerFactory)
    : BaseCommandHandler<TCommand>(loggerFactory) where TCommand : ICommand<Result>
{
    protected static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
    protected readonly IGenericRepository repository = repository;

    protected override void OnFailure()
    {
        repository.ClearChanges();
    }

    protected static string Digits(string s)
    {
        return string.Concat(s.Select(c =>
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
    }

    protected static int Num(string s)
    {
        return int.TryParse(Digits(s), out var v) && v > 0
            ? v
            : throw new FormatException("عدد مثبت وارد کنید.");
    }

    protected static Guid ParseId(string value) => Guid.TryParse(value.Trim(), out var id) && id != Guid.Empty ? id : throw new FormatException("شناسه معتبر وارد کنید.");

    protected static DateTime ParseLocal(string s)
    {
        if (!DateTime.TryParseExact(Digits(s), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var d)) throw new FormatException("تاریخ معتبر میلادی با قالب خواسته‌شده وارد کنید.");
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), Tehran);
    }

    public static DateTime Local(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tehran);
    }

    protected async Task<User> FindUser(string s)
    {
        s = Digits(s.Trim().TrimStart('@'));
        var u = long.TryParse(s, out var id)
            ? await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == id)
            : await repository.Query<User>()
                .FirstOrDefaultAsync(x => x.Username != null && x.Username.ToLower() == s.ToLower());
        return u ?? throw new InvalidOperationException("کاربر ثبت‌نام‌شده پیدا نشد.");
    }
}