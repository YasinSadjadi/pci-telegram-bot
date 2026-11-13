using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VocabularyBot.Application.Students.RegisterStudent;
using VocabularyBot.Application.Courses;
using VocabularyBot.Application.Workflows.AssignAdmin;
using VocabularyBot.Application.Words.ListMyWords;
using VocabularyBot.Application.Workflows.RegisterWord;
using VocabularyBot.Domain;
using VocabularyBot.Domain.Words;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Conversations;

public sealed class BotService(IGenericRepository repository, IMediator mediator, BotOptions options)
{
    private const string Help = "دستورها:\n/register ثبت شماره دانشجویی\n/classes کلاس‌های من\n/join انتخاب کلاس\n/course ایجاد کلاس (ادمین)\n/admin واگذاری نقش ادمین\n/word ثبت واژه\n/words فهرست واژه‌های من\n/cancel لغو عملیات\n/help راهنما";
    private static readonly string[] CoursePrompts = ["نام دانشگاه را وارد کنید:", "سال ترم را وارد کنید:", "شماره ترم را وارد کنید:"];
    private static readonly string[] WordPrompts =
    [
        "شناسه کلاس خود را از /classes وارد کنید:",
        "کلمه یا عبارت انگلیسی را وارد کنید:",
        "تلفظ را وارد کنید:",
        "معنی فارسی را وارد کنید:",
        "خانواده واژه را وارد کنید یا /skip بفرستید:",
        "مترادف‌ها را وارد کنید یا /skip بفرستید:",
        "ترجمه به زبان‌های دیگر را وارد کنید (مثلاً آلمانی: ...، اسپانیایی: ...) یا /skip بفرستید:"
    ];

    public async Task<string> Handle(long id, string? username, string name, string input)
    {
        input = input.Trim();
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == id);
        if (input == "/start")
        {
            if (user is null) { user = new User { TelegramId = id }; repository.Create(user); }
            user.Name = name;
            user.Username = username;
            if (id == options.BootstrapAdminId) user.IsAdmin = true;
            await repository.SaveChangesAsync();
            if (user.StudentNumber is null) { await StartFlow(user.Id, "register"); return "خوش آمدید. شماره دانشجویی خود را وارد کنید:"; }
            if (!await repository.Query<CourseMembership>().AnyAsync(m => m.UserId == user.Id) && !user.IsAdmin)
            {
                await StartFlow(user.Id, "select_class");
                return await ClassSelectionPrompt();
            }
            return "خوش آمدید.\n" + Help;
        }
        if (user is null) return "ابتدا /start را ارسال کنید.";
        if (input == "/cancel") { await ClearFlow(user.Id); return "عملیات لغو شد."; }
        if (input is "/help" or "/menu") return Help;
        if (input == "/register") { await StartFlow(user.Id, "register"); return "شماره دانشجویی خود را وارد کنید:"; }
        if (input == "/classes") return await mediator.Send(new ListCoursesQuery(id));
        if (input == "/join")
        {
            if (user.StudentNumber is null) return "ابتدا شماره دانشجویی خود را با /register ثبت کنید.";
            await StartFlow(user.Id, "select_class");
            return await ClassSelectionPrompt();
        }
        if (input == "/admin")
        {
            if (!user.IsAdmin) return "این دستور مخصوص ادمین است. برای دسترسی با ادمین هماهنگ کنید.";
            await StartFlow(user.Id, "admin");
            return "شناسه عددی تلگرام یا نام کاربری فرد ثبت‌نام‌شده را وارد کنید:";
        }
        if (input == "/course")
        {
            if (!user.IsAdmin) return "این دستور مخصوص ادمین است. برای دسترسی با ادمین هماهنگ کنید.";
            await StartFlow(user.Id, "course");
            return CoursePrompts[0];
        }
        if (input == "/words") return await mediator.Send(new ListMyWordsQuery(id));
        if (input == "/word")
        {
            if (user.StudentNumber is null) return "ابتدا شماره دانشجویی خود را ثبت کنید: /register";
            await StartFlow(user.Id, "word");
            return WordPrompts[0];
        }
        if (input != "/skip" && System.Text.RegularExpressions.Regex.IsMatch(input, @"^/[A-Za-z_]+$")) return "دستور ناشناخته است. /help";
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is null) return "برای شروع /help را بفرستید.";
        if (state.Flow == "register")
        {
            var result = await mediator.Send(new RegisterStudentCommand(id, input));
            if (!result.IsSuccessful) return result.Message;
            await StartFlow(user.Id, "select_class");
            return result.Message + "\n" + await ClassSelectionPrompt();
        }
        if (state.Flow == "admin")
        {
            if (!user.IsAdmin) { await ClearFlow(user.Id); return "این دستور مخصوص ادمین است."; }
            var result = await mediator.Send(new AssignAdminCommand(id, [input]));
            if (result.IsSuccessful) { await ClearFlow(user.Id); return result.Message; }
            return result.Message + "\nشناسه را دوباره وارد کنید یا /cancel را بفرستید.";
        }
        if (state.Flow == "select_class")
        {
            var result = await mediator.Send(new JoinCourseCommand(id, input));
            if (result.IsSuccessful) { await ClearFlow(user.Id); return result.Message; }
            return result.Message + "\nبرای دیدن دوباره فهرست /join را بفرستید یا با /cancel خارج شوید.";
        }
        if (state.Flow == "course")
        {
            if (!user.IsAdmin) { await ClearFlow(user.Id); return "این دستور مخصوص ادمین است."; }
            var fields = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
            if (string.IsNullOrWhiteSpace(input) || input.Length > 2000 || input == "/skip") return "این فیلد اجباری است. لطفاً مقدار معتبر وارد کنید.";
            if (fields.Count > 0)
            {
                var digits = Digits.Convert(input);
                if (!int.TryParse(digits, out var value) || value <= 0) return "این مقدار باید عدد مثبت باشد. لطفاً دوباره وارد کنید.";
            }
            if (fields.Count == 0 && input.Length > 200) return "نام دانشگاه نباید بیش از ۲۰۰ نویسه باشد. لطفاً نام کوتاه‌تری وارد کنید.";
            fields.Add(input);
            if (fields.Count < CoursePrompts.Length) { state.Data = JsonSerializer.Serialize(fields); state.Step = fields.Count; await repository.SaveChangesAsync(); return CoursePrompts[state.Step]; }
            var result = await mediator.Send(new CreateCourseCommand(id, fields[0], fields[1], fields[2]));
            if (result.IsSuccessful) { await ClearFlow(user.Id); return result.Message; }
            return result.Message + "\nلطفاً مقدار این مرحله را دوباره وارد کنید یا /cancel را بفرستید.";
        }
        if (state.Flow != "word") { await ClearFlow(user.Id); return "این عملیات دیگر پشتیبانی نمی‌شود. دوباره شروع کنید."; }
        var values = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
        if (input == "/skip" && values.Count < 4) return "این فیلد اجباری است. لطفاً مقدار آن را وارد کنید.";
        if (input.Length == 0 || input.Length > 2000) return "متن واردشده باید بین ۱ تا ۲۰۰۰ نویسه باشد. لطفاً دوباره وارد کنید.";
        if (values.Count == 0)
        {
            if (!Guid.TryParse(input, out var courseId) || !await repository.Query<CourseMembership>().AnyAsync(m => m.CourseId == courseId && m.UserId == user.Id))
                return "شناسه کلاس معتبر نیست یا شما عضو آن نیستید. شناسه را از /classes کپی کنید یا با ادمین تماس بگیرید.";
        }
        if (values.Count == 1 && input != "/skip")
        {
            var normalized = WordNormalizer.Normalize(input);
            if (await repository.Query<VocabularyEntry>().AnyAsync(x => x.NormalizedWord == normalized) ||
                await repository.Query<Word>().AnyAsync(x => x.NormalizedTitle == normalized))
                return "این واژه قبلاً ثبت شده است. لطفاً واژه دیگری وارد کنید یا /cancel را بفرستید.";
        }
        values.Add(input == "/skip" ? "" : input);
        if (values.Count < WordPrompts.Length)
        {
            state.Data = JsonSerializer.Serialize(values);
            state.Step = values.Count;
            await repository.SaveChangesAsync();
            return WordPrompts[state.Step];
        }
        var response = await mediator.Send(new RegisterWordCommand(id, values));
        if (response.IsSuccessful) { await ClearFlow(user.Id); return response.Message; }
        return response.Message + "\nمی‌توانید دوباره تلاش کنید یا با /cancel فرم را ببندید.";
    }

    private async Task<string> ClassSelectionPrompt()
    {
        var courses = await repository.Query<Course>(false).OrderBy(c => c.University).ThenBy(c => c.Year).ThenBy(c => c.Term).ToListAsync();
        if (courses.Count == 0) return "هنوز کلاسی ایجاد نشده است. پس از ایجاد کلاس توسط ادمین، /join را بفرستید.";
        return "برای انتخاب کلاس، شناسه کامل آن را از فهرست کپی و ارسال کنید:\n" +
               string.Join("\n", courses.Select(c => $"{c.Id} | {c.University} | {c.Year}-{c.Term}"));
    }

    private async Task StartFlow(Guid userId, string flow)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == userId);
        if (state is null) repository.Create(new Conversation { UserId = userId, Flow = flow });
        else { state.Flow = flow; state.Step = 0; state.Data = "[]"; }
        await repository.SaveChangesAsync();
    }

    private async Task ClearFlow(Guid userId)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == userId);
        if (state is not null) { repository.Delete(state); await repository.SaveChangesAsync(); }
    }
}
