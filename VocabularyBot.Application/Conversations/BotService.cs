using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VocabularyBot.Application.Students.RegisterStudent;
using VocabularyBot.Application.Courses;
using VocabularyBot.Application.Workflows.AssignAdmin;
using VocabularyBot.Application.Words.ListMyWords;
using VocabularyBot.Application.Words;
using VocabularyBot.Application.Workflows.RegisterWord;
using VocabularyBot.Domain;
using VocabularyBot.Domain.Words;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Conversations;

public sealed class BotService(IGenericRepository repository, IMediator mediator, BotOptions options)
{
    private static string Help(bool admin) => "📖 راهنمای واژه‌یار\n\n۱. شماره دانشجویی‌تان را ثبت کنید.\n۲. کلاس خود را انتخاب کنید.\n۳. واژه‌ها را با فرم مرحله‌ای ثبت کنید.\n\nمی‌توانید از دکمه‌های پایین یا این دستورها استفاده کنید:\n/word — ثبت واژه\n/words — واژه‌های من\n/classes — کلاس‌های من\n/join — عضویت در کلاس\n/register — تغییر شماره دانشجویی\n\nداخل فرم:\n/back — اصلاح مرحله قبل\n/cancel — لغو فرم\n/menu — خروج از فرم و منوی اصلی" +
        (admin ? "\n\n🔑 مدیریت\n/course — ساخت کلاس\n/admin — افزودن ادمین" : "");
    private static readonly string[] CoursePrompts =
    [
        "🏫 ساخت کلاس · مرحله ۱ از ۳\n\nنام دانشگاه را وارد کنید:",
        "🏫 ساخت کلاس · مرحله ۲ از ۳\n\nسال ترم را وارد کنید؛ مثلاً ۱۴۰۵:",
        "🏫 ساخت کلاس · مرحله ۳ از ۳\n\nشماره ترم را وارد کنید؛ مثلاً ۱:"
    ];
    private static readonly string[] WordPrompts =
    [
        "🏫 انتخاب کلاس واژه\n\nواژه برای کدام کلاس ثبت شود؟ یکی از دکمه‌های زیر را انتخاب کنید.",
        "✍️ ثبت واژه · مرحله ۱ از ۶\n\nکلمه یا عبارت انگلیسی را وارد کنید.\nمثال: example",
        "✍️ ثبت واژه · مرحله ۲ از ۶\n\nتلفظ را وارد کنید.\nمثال: /ɪɡˈzɑːmpəl/",
        "✍️ ثبت واژه · مرحله ۳ از ۶\n\nمعنی فارسی را وارد کنید.\nمثال: نمونه، مثال",
        "✍️ ثبت واژه · مرحله ۴ از ۶\n\nخانواده واژه را وارد کنید.\nمثال: exemplify, exemplary",
        "✍️ ثبت واژه · مرحله ۵ از ۶\n\nمترادف‌ها را وارد کنید.\nمثال: instance, sample\n\nاختیاری است؛ دکمه «بدون مترادف» یا /skip را بفرستید.",
        $"✍️ ثبت واژه · مرحله ۶ از ۶\n\nترجمه به زبان دیگر (اختیاری)\nهر زبان را در یک خط بنویسید؛ مثلاً:\nآلمانی: Beispiel\nفرانسوی: exemple\n\nزبان‌های پشتیبانی‌شده: {TranslationInput.SupportedLanguages}\n\nبرای ذخیره، دکمه «ثبت واژه» یا /done را بفرستید."

    ];

    public async Task<string> Handle(long id, string? username, string name, string input)
    {
        input = BotMenu.Command(input.Trim());
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == id);
        if (input.StartsWith("class:") || input.StartsWith("page:"))
        {
            var current = user is null ? null : await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            var parts = input.Split(':');
            if (current is null || parts.Length != 3 || parts[1] != SelectionKey(current) ||
                !(current.Flow == "select_class" || current.Flow == "word" && current.Step == 0))
                return "این انتخاب دیگر فعال نیست. برای انتخاب کلاس /join یا برای ثبت واژه /word را بفرستید.";
            if (parts[0] == "page") return current.Flow == "word" ? WordPrompts[0] : await ClassSelectionPrompt();
            input = parts[2] == "cancel" ? "/cancel" : parts[2];
        }
        if (input == "/start")
        {
            if (user is null) { user = new User { TelegramId = id }; repository.Create(user); }
            user.Name = name;
            user.Username = username;
            if (id == options.BootstrapAdminId ||
                (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(options.BootstrapAdminUsername) &&
                 string.Equals(username.TrimStart('@'), options.BootstrapAdminUsername.Trim().TrimStart('@'), StringComparison.OrdinalIgnoreCase) &&
                 !await repository.Query<User>().AnyAsync(x => x.IsAdmin)))
                user.IsAdmin = true;
            await repository.SaveChangesAsync();
            if (user.StudentNumber is null) { await StartFlow(user.Id, "register"); return "👋 به واژه‌یار خوش آمدید\n\nواژه‌های کلاس را اینجا ثبت و مرور کنید.\nبرای شروع، شماره دانشجویی خود را وارد کنید.\nارقام فارسی و انگلیسی پذیرفته می‌شوند."; }
            if (!await repository.Query<CourseMembership>().AnyAsync(m => m.UserId == user.Id) && !user.IsAdmin)
            {
                await StartFlow(user.Id, "select_class");
                return await ClassSelectionPrompt();
            }
            await ClearFlow(user.Id);
            return Home(user);
        }
        if (user is null) return "ابتدا /start را ارسال کنید.";
        if (input == "/cancel") { await ClearFlow(user.Id); return "عملیات لغو شد.\n\n" + Home(user); }
        if (input == "/menu") { await ClearFlow(user.Id); return Home(user); }
        if (input == "/help") return Help(user.IsAdmin);
        if (input == "/back") return await Back(user);
        if (input == "/register") { await StartFlow(user.Id, "register"); return "👤 شماره دانشجویی\n\nشماره دانشجویی خود را وارد کنید.\nارقام فارسی و انگلیسی پذیرفته می‌شوند."; }
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
            var memberships = await repository.Query<CourseMembership>(false).Where(m => m.UserId == user.Id).Select(m => m.CourseId).ToListAsync();
            if (memberships.Count == 0) return "ابتدا با /join کلاس خود را انتخاب کنید.";
            await StartFlow(user.Id, "word");
            if (memberships.Count == 1)
            {
                var wordState = await repository.Query<Conversation>().SingleAsync(c => c.UserId == user.Id);
                wordState.Data = JsonSerializer.Serialize(new[] { memberships[0].ToString() });
                wordState.Step = 1;
                await repository.SaveChangesAsync();
                return WordPrompts[1];
            }
            return WordPrompts[0];
        }
        if (input is not ("/skip" or "/done") && System.Text.RegularExpressions.Regex.IsMatch(input, @"^/[A-Za-z_]+$")) return "دستور ناشناخته است. /help";
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is null) return Home(user);
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
            if (string.IsNullOrWhiteSpace(input) || input.Length > 2000 || input is "/skip" or "/done") return "این فیلد اجباری است. لطفاً مقدار معتبر وارد کنید.";
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
        if (values.Count >= 6)
        {
            if (input == "/done")
            {
                var completed = await mediator.Send(new RegisterWordCommand(id, values));
                if (completed.IsSuccessful) { await ClearFlow(user.Id); return $"✅ واژه ثبت شد.\n\n{values[1]}\nمعنی فارسی: {values[3]}\n\nاز «واژه‌های من» مرور کنید یا یک واژه دیگر ثبت کنید."; }
                return completed.Message + "\nمی‌توانید دوباره تلاش کنید یا با /cancel فرم را ببندید.";
            }
            if (input == "/skip") return "برای پایان ورود ترجمه‌ها /done را بفرستید.";
            if (input.Length == 0 || input.Length > 2000) return "متن ترجمه باید بین ۱ تا ۲۰۰۰ نویسه باشد.";
            var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0) return "هر ترجمه را به شکل «زبان: ترجمه» وارد کنید.";
            var parsed = new List<(LanguageType Type, string Meaning)>();
            foreach (var line in lines)
            {
                if (!TranslationInput.TryParse(line, out var type, out var meaning)) return $"زبان یا قالب ترجمه معتبر نیست. به شکل «آلمانی: Beispiel» وارد کنید. زبان‌های پشتیبانی‌شده: {TranslationInput.SupportedLanguages}.";
                parsed.Add((type, meaning));
            }
            foreach (var item in parsed)
            {
                var index = values.FindIndex(6, line => TranslationInput.TryParse(line, out var previousType, out _) && previousType == item.Type);
                var line = $"{TranslationInput.Name(item.Type)}: {item.Meaning}";
                if (index < 0) values.Add(line); else values[index] = line;
            }
            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return "✓ ترجمه به فرم اضافه شد.\n\n" + string.Join("\n", values.Skip(6)) + "\n\nبرای اصلاح، همان زبان را دوباره بفرستید.\nبرای ذخیره نهایی، «ثبت واژه» یا /done را انتخاب کنید.";
        }
        if (input == "/done") return "ابتدا فیلدهای اجباری و مرحله مترادف را تکمیل کنید.";
        if (input == "/skip" && values.Count < 5) return "این فیلد اجباری است. لطفاً مقدار آن را وارد کنید.";
        if (input.Length == 0 || input.Length > 2000) return "متن واردشده باید بین ۱ تا ۲۰۰۰ نویسه باشد. لطفاً دوباره وارد کنید.";
        if (values.Count == 0)
        {
            if (!Guid.TryParse(input, out var courseId) || !await repository.Query<CourseMembership>().AnyAsync(m => m.CourseId == courseId && m.UserId == user.Id))
                return "کلاس معتبر نیست یا شما عضو آن نیستید. یکی از دکمه‌های کلاس را انتخاب کنید.";
        }
        if (values.Count == 1 && input != "/skip")
        {
            var normalized = WordNormalizer.Normalize(input);
            if (await repository.Query<VocabularyEntry>().AnyAsync(x => x.NormalizedWord == normalized))
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
        return "برای پایان ثبت ترجمه‌ها /done را بفرستید.";
    }

    private async Task<string> ClassSelectionPrompt()
    {
        if (!await repository.Query<Course>(false).AnyAsync()) return "هنوز کلاسی ایجاد نشده است. پس از ایجاد کلاس توسط ادمین، /join را بفرستید.";
        return "🏫 انتخاب کلاس\n\nدانشگاه و ترم خود را از دکمه‌های زیر انتخاب کنید.\nبا انتخاب کلاس، عضویت شما ثبت می‌شود.";
    }

    private static string Home(User user) => "🏠 واژه‌یار\n\n" +
        (user.StudentNumber is null ? "برای شروع، «شماره دانشجویی» را انتخاب کنید." : "برای ثبت واژه جدید یا مرور واژه‌ها، از منوی پایین انتخاب کنید.");

    private static string SelectionKey(Conversation state) =>
        (state.Flow == "word" ? "w" : "j") + state.Id.ToString("N")[..8];

    private async Task<string> Back(User user)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is null) return Home(user);
        var values = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
        if (state.Flow == "course" && values.Count > 0)
        {
            values.RemoveAt(values.Count - 1);
            state.Step = values.Count;
            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return CoursePrompts[state.Step];
        }
        if (state.Flow == "word" && values.Count > 0)
        {
            if (values.Count == 1 && await repository.Query<CourseMembership>().CountAsync(m => m.UserId == user.Id) <= 1)
                return "در اولین مرحله هستید.\n\n" + WordPrompts[1];
            var previous = Math.Min(values.Count - 1, 5);
            values.RemoveRange(previous, values.Count - previous);
            state.Step = values.Count;
            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return (previous == 5 ? "مترادف و ترجمه‌ها را دوباره وارد کنید.\n\n" : "") + WordPrompts[state.Step];
        }
        return "در اولین مرحله هستید. برای خروج، «لغو» یا /cancel را بفرستید.";
    }

    public async Task<BotKeyboard> Keyboard(long telegramId, string input)
    {
        var user = await repository.Query<User>(false).FirstOrDefaultAsync(x => x.TelegramId == telegramId);
        if (user is null) return BotMenu.Reply(["/start"]);
        var state = await repository.Query<Conversation>(false).FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is not null)
        {
            if (state.Flow == "select_class" || state.Flow == "word" && state.Step == 0)
            {
                var query = repository.Query<Course>(false);
                if (state.Flow == "word")
                    query = query.Where(c => repository.Query<CourseMembership>(false).Any(m => m.CourseId == c.Id && m.UserId == user.Id));
                const int pageSize = 8;
                var count = await query.CountAsync();
                var parts = input.Split(':');
                var page = parts.Length == 3 && parts[0] == "page" && parts[1] == SelectionKey(state) && int.TryParse(parts[2], out var requested)
                    ? Math.Clamp(requested, 0, Math.Max(0, (count - 1) / pageSize)) : 0;
                var courses = await query.OrderBy(c => c.University).ThenBy(c => c.Year).ThenBy(c => c.Term).ThenBy(c => c.Id)
                    .Skip(page * pageSize).Take(pageSize).ToListAsync();
                if (courses.Count == 0) return BotMenu.Reply([BotMenu.Join], [BotMenu.Cancel]);
                var rows = courses.Select(c => (IReadOnlyList<BotButton>)new[]
                {
                    new BotButton($"{c.University} · {c.Year} · ترم {c.Term}", $"class:{SelectionKey(state)}:{c.Id:N}")
                }).ToList();
                var navigation = new List<BotButton>();
                if (page > 0) navigation.Add(new("◀ صفحه قبل", $"page:{SelectionKey(state)}:{page - 1}"));
                if ((page + 1) * pageSize < count) navigation.Add(new("صفحه بعد ▶", $"page:{SelectionKey(state)}:{page + 1}"));
                if (navigation.Count > 0) rows.Add(navigation);
                rows.Add([new BotButton(BotMenu.Cancel, $"class:{SelectionKey(state)}:cancel")]);
                return new BotKeyboard(rows, Inline: true);
            }
            var controls = new List<string[]>();
            if (state.Flow == "word" && state.Step == 5) controls.Add([BotMenu.Skip]);
            if (state.Flow == "word" && state.Step >= 6) controls.Add([BotMenu.Done]);
            var canGoBack = state.Flow == "course" && state.Step > 0 ||
                state.Flow == "word" && (state.Step > 1 || state.Step == 1 &&
                    await repository.Query<CourseMembership>(false).CountAsync(m => m.UserId == user.Id) > 1);
            if (canGoBack) controls.Add([BotMenu.Back, BotMenu.Cancel]);
            else controls.Add([BotMenu.Cancel]);
            controls.Add([BotMenu.Help]);
            return BotMenu.Reply(controls.ToArray()) with { Placeholder = "پاسخ این مرحله را بنویسید…" };
        }
        var menu = new List<string[]>();
        if (user.StudentNumber is not null)
        {
            menu.Add([BotMenu.Word, BotMenu.Words]);
            menu.Add([BotMenu.Classes, BotMenu.Join]);
        }
        menu.Add([BotMenu.Register, BotMenu.Help]);
        if (user.IsAdmin) menu.Add([BotMenu.Course, BotMenu.Admin]);
        return BotMenu.Reply(menu.ToArray());
    }

    private async Task StartFlow(Guid userId, string flow)
    {
        // A fresh conversation identity also expires buttons from previous forms.
        await ClearFlow(userId);
        repository.Create(new Conversation { UserId = userId, Flow = flow });
        await repository.SaveChangesAsync();
    }

    private async Task ClearFlow(Guid userId)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == userId);
        if (state is not null) { repository.Delete(state); await repository.SaveChangesAsync(); }
    }
}
