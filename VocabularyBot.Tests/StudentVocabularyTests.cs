using VocabularyBot.Domain.Words;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application;
using VocabularyBot.Application.Conversations;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;

namespace VocabularyBot.Tests;

public sealed class StudentVocabularyTests
{
    private sealed class TestEnvironment : IAsyncDisposable
    {
        private readonly string path = Path.GetTempFileName();
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;
        public BotDb Db => scope.ServiceProvider.GetRequiredService<BotDb>();
        public IMediator Mediator => scope.ServiceProvider.GetRequiredService<IMediator>();
        public TestEnvironment(string? adminUsername = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={path};Pooling=False"));
            services.AddInfrastructure();
            services.AddApplication(1, adminUsername);
            provider = services.BuildServiceProvider();
            scope = provider.CreateAsyncScope();
        }
        public Task Initialize() => Db.Database.MigrateAsync();
        public Task<BotReply> Reply(long id, string text) => Mediator.Send(new ProcessMessageCommand(id, "user" + id, "Student", text));
        public async Task<string> Send(long id, string text) => (await Reply(id, text)).Text;
        public async ValueTask DisposeAsync() { await scope.DisposeAsync(); await provider.DisposeAsync(); File.Delete(path); }
    }

    [Fact]
    public async Task Username_bootstraps_only_the_first_admin_case_insensitively()
    {
        await using var env = new TestEnvironment("example_admin");
        await env.Initialize();
        await env.Mediator.Send(new ProcessMessageCommand(10, "example_admin", "Admin", "/start"));
        await env.Mediator.Send(new ProcessMessageCommand(11, "example_admin", "Other", "/start"));
        Assert.True((await env.Db.Users.SingleAsync(x => x.TelegramId == 10)).IsAdmin);
        Assert.False((await env.Db.Users.SingleAsync(x => x.TelegramId == 11)).IsAdmin);
    }

    private static async Task<Course> JoinClass(TestEnvironment env, params long[] telegramIds)
    {
        var course = new Course { University = "University", Year = 404, Term = 1 };
        env.Db.Courses.Add(course);
        foreach (var telegramId in telegramIds)
        {
            var user = await env.Db.Users.SingleAsync(x => x.TelegramId == telegramId);
            env.Db.CourseMemberships.Add(new CourseMembership { CourseId = course.Id, UserId = user.Id });
        }
        await env.Db.SaveChangesAsync();
        return course;
    }

    [Fact]
    public async Task Student_number_is_required_normalized_unique_and_keeps_leading_zeroes()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        Assert.Contains("شماره دانشجویی", await env.Send(10, "/start"));
        Assert.Contains("فقط شامل ارقام", await env.Send(10, "abc"));
        Assert.Contains("انجام شد", await env.Send(10, "۰۰۱۲۳"));
        Assert.Equal("00123", (await env.Db.Users.SingleAsync()).StudentNumber);
        await env.Send(11, "/start");
        Assert.Contains("قبلاً", await env.Send(11, "00123"));
        Assert.Contains("انجام شد", await env.Send(11, "456"));
        Assert.Equal(2, await env.Db.Users.CountAsync(x => x.StudentNumber != null));
    }

    [Fact]
    public async Task Required_fields_cannot_be_skipped_and_optional_fields_can_be_omitted()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        await env.Send(10, "/word");
        Assert.Contains("اجباری", await env.Send(10, "/skip"));
        await env.Send(10, "Eventual Consistency");
        Assert.Contains("اجباری", await env.Send(10, "/skip"));
        await env.Send(10, "/ɪˈventʃuəl/");
        Assert.Contains("اجباری", await env.Send(10, "/skip"));
        await env.Send(10, "سازگاری نهایی");
        Assert.Contains("مرحله ۵", await env.Send(10, "/skip"));
        await env.Send(10, "/skip");
        Assert.Contains("ثبت شد", await env.Send(10, "/done"));
        var entry = await env.Db.VocabularyEntries.SingleAsync();
        Assert.Equal("Eventual Consistency", entry.EnglishWord); Assert.Equal("/ɪˈventʃuəl/", entry.Pronunciation);
        Assert.Equal("", entry.Family); Assert.Null(entry.Synonyms);
        Assert.Empty(await env.Db.WordTranslations.ToListAsync());
        Assert.Contains("Eventual Consistency", await env.Send(10, "/words"));
    }

    [Fact]
    public async Task Complete_form_persists_optional_fields_and_duplicate_is_global()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        await env.Send(11, "/start"); await env.Send(11, "101");
        var course = await JoinClass(env, 10, 11);
        async Task<string> Word(long user, string title)
        {
            await env.Send(user, "/word"); await env.Send(user, title); await env.Send(user, "pronunciation");
            await env.Send(user, "معنی"); await env.Send(user, "family"); await env.Send(user, "synonym");
            await env.Send(user, "آلمانی: Beispiel\nفرانسوی: exemple");
            await env.Send(user, "اسپانیایی: ejemplo");
            return await env.Send(user, "/done");
        }
        Assert.Contains("ثبت شد", await Word(10, "  Example   Phrase "));
        await env.Send(11, "/word");
        Assert.Contains("قبلاً", await env.Send(11, "example phrase"));
        await env.Send(11, "/cancel");
        var word = await env.Db.VocabularyEntries.SingleAsync();
        Assert.Equal("family", word.Family); Assert.Equal("synonym", word.Synonyms);
        var translations = await env.Db.WordTranslations.Where(x => x.VocabularyEntryId == word.Id).ToListAsync();
        Assert.Equal(3, translations.Count);
        Assert.Contains(translations, x => x.Type == LanguageType.German && x.Meaning == "Beispiel");
        Assert.Contains(translations, x => x.Type == LanguageType.Spanish && x.Meaning == "ejemplo");
        var list = await env.Send(10, "/words");
        Assert.Contains("خانواده واژه: family", list);
        Assert.Contains("آلمانی: Beispiel", list);
        Assert.Contains("فرانسوی: exemple", list);
        Assert.Contains("اسپانیایی: ejemplo", list);
        Assert.DoesNotContain("Example", await env.Send(11, "/words"));
    }

    [Fact]
    public async Task Unregistered_users_cannot_enter_word_and_group_commands_are_unavailable()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        Assert.Contains("ابتدا /start", await env.Send(10, "/word"));
        await env.Send(10, "/start");
        Assert.Contains("شماره دانشجویی", await env.Send(10, "/word"));
        await env.Send(10, "100");
        foreach (var command in new[] { "/group", "/week", "/leader", "/groups", "/status" })
            Assert.Contains("ناشناخته", await env.Send(10, command));
        Assert.Contains("/join", await env.Send(10, "/word"));
        Assert.Contains("لغو", await env.Send(10, "/cancel"));
        Assert.Empty(await env.Db.Conversations.ToListAsync());
    }
    [Fact]
    public async Task Database_unique_index_enforces_global_word_uniqueness()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        await env.Send(11, "/start"); await env.Send(11, "101");
        var users = await env.Db.Users.OrderBy(x => x.TelegramId).ToListAsync();
        var course = await JoinClass(env, 10, 11);
        env.Db.VocabularyEntries.Add(new VocabularyEntry { CourseId = course.Id, UserId = users[0].Id, EnglishWord = "TEST", NormalizedWord = "TEST", Pronunciation = "test", PersianMeaning = "معنی" });
        await env.Db.SaveChangesAsync();
        env.Db.VocabularyEntries.Add(new VocabularyEntry { CourseId = course.Id, UserId = users[1].Id, EnglishWord = "test", NormalizedWord = "TEST", Pronunciation = "test", PersianMeaning = "معنی" });
        await Assert.ThrowsAsync<DbUpdateException>(() => env.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Student_selects_a_class_during_registration_and_can_join_another_class()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var first = new Course { University = "First", Year = 404, Term = 1 };
        var second = new Course { University = "Second", Year = 404, Term = 2 };
        env.Db.Courses.AddRange(first, second); await env.Db.SaveChangesAsync();
        await env.Send(10, "/start");
        var list = await env.Reply(10, "100");
        Assert.Contains(list.Keyboard!.Rows.SelectMany(x => x), b => b.CallbackData!.EndsWith(first.Id.ToString("N")));
        Assert.Contains(list.Keyboard.Rows.SelectMany(x => x), b => b.CallbackData!.EndsWith(second.Id.ToString("N")));
        Assert.Contains("پیدا نشد", await env.Send(10, "wrong"));
        Assert.Contains("عضویت", await env.Send(10, first.Id.ToString()));
        Assert.Contains(first.University, await env.Send(10, "/classes"));
        Assert.DoesNotContain(second.University, await env.Send(10, "/classes"));
        await env.Send(10, "/join");
        Assert.Contains("عضویت", await env.Send(10, second.Id.ToString()));
        Assert.Equal(2, await env.Db.CourseMemberships.CountAsync());
        await env.Send(10, "/join");
        Assert.Contains("از قبل عضو", await env.Send(10, first.Id.ToString()));
        Assert.Equal(2, await env.Db.CourseMemberships.CountAsync());
    }

    [Fact]
    public async Task Admin_creates_class_and_invalid_input_can_be_corrected()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(1, "/start"); await env.Send(1, "111");
        await env.Send(10, "/start"); await env.Send(10, "100");
        Assert.Contains("مخصوص ادمین", await env.Send(10, "/course"));
        await env.Send(1, "/course"); await env.Send(1, "University");
        Assert.Contains("عدد مثبت", await env.Send(1, "سال نامعتبر"));
        await env.Send(1, "۴۰۴");
        Assert.Contains("ثبت شد", await env.Send(1, "۱"));
        Assert.Single(await env.Db.Courses.ToListAsync());
    }

    [Fact]
    public async Task Registration_handles_empty_class_list_and_join_after_course_is_created()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start");
        Assert.Contains("هنوز کلاسی", await env.Send(10, "100"));
        var course = new Course { University = "U", Year = 404, Term = 1 };
        env.Db.Courses.Add(course); await env.Db.SaveChangesAsync();
        var choices = await env.Reply(10, "/join");
        Assert.Contains(choices.Keyboard!.Rows.SelectMany(x => x), b => b.CallbackData!.EndsWith(course.Id.ToString("N")));
        Assert.Contains("عضویت", await env.Send(10, course.Id.ToString()));
    }

    [Fact]
    public async Task Word_form_rejects_nonmember_and_preserves_the_current_step()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        await JoinClass(env, 10);
        await env.Send(10, "/word");
        Assert.Contains("عضو آن نیستید", await env.Send(10, Guid.NewGuid().ToString()));
        Assert.Contains("کلمه", await env.Send(10, course.Id.ToString()));
    }

    [Fact]
    public async Task Unexpected_persistence_failure_produces_safe_message_without_technical_details()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Db.DisposeAsync();
        var response = await env.Mediator.Send(new VocabularyBot.Application.Courses.CreateCourseCommand(1, "University", "404", "1"));
        Assert.False(response.IsSuccessful);
        Assert.Contains("دوباره تلاش", response.Message);
        Assert.DoesNotContain("Exception", response.Message);
        Assert.DoesNotContain("DbContext", response.Message);
        var message = await env.Send(1, "/start");
        Assert.Contains("دوباره تلاش", message);
        Assert.DoesNotContain("Exception", message);
        Assert.DoesNotContain("DbContext", message);
    }

    [Fact]
    public async Task Admin_can_assign_admin_and_unknown_user_error_allows_retry()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(1, "/start"); await env.Send(1, "111");
        await env.Send(10, "/start"); await env.Send(10, "100");
        await env.Send(1, "/admin");
        Assert.Contains("پیدا نشد", await env.Send(1, "missing"));
        Assert.Contains("ادمین", await env.Send(1, "user10"));
        Assert.True((await env.Db.Users.SingleAsync(x => x.TelegramId == 10)).IsAdmin);
    }

    [Fact]
    public async Task Student_registers_selects_class_then_adds_a_word_without_any_group_step()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var course = new Course { University = "U", Year = 404, Term = 1 };
        env.Db.Courses.Add(course); await env.Db.SaveChangesAsync();
        Assert.Contains("شماره دانشجویی", await env.Send(10, "/start"));
        var choices = await env.Reply(10, "1234");
        var choice = choices.Keyboard!.Rows.SelectMany(x => x).Single(b => b.CallbackData!.EndsWith(course.Id.ToString("N")));
        Assert.Contains("عضویت", await env.Send(10, choice.CallbackData!));
        Assert.Contains("کلمه", await env.Send(10, "/word"));
        await env.Send(10, "Computer Science");
        await env.Send(10, "/kəmˈpjuːtər/");
        await env.Send(10, "علوم کامپیوتر");
        await env.Send(10, "computer, computing");
        await env.Send(10, "/skip");
        Assert.Contains("ثبت شد", await env.Send(10, "/done"));
        Assert.Equal(course.Id, (await env.Db.VocabularyEntries.SingleAsync()).CourseId);
    }

    [Fact]
    public async Task Translations_validate_enum_languages_and_allow_corrections_without_losing_draft()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100"); await JoinClass(env, 10);
        await env.Send(10, "/word"); await env.Send(10, "example"); await env.Send(10, "pronunciation");
        await env.Send(10, "معنی"); await env.Send(10, "family"); await env.Send(10, "/skip");
        Assert.Contains("قالب", await env.Send(10, "ترجمه بدون زبان"));
        Assert.Contains("پشتیبانی‌شده", await env.Send(10, "ژاپنی: 例"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "آلمانی: Beispiel"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "German: Muster"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "فرانسوی: exemple"));
        Assert.Contains("ثبت شد", await env.Send(10, "/done"));
        var translations = await env.Db.WordTranslations.ToListAsync();
        Assert.Equal(2, translations.Count);
        Assert.Contains(translations, t => t.Type == LanguageType.German && t.Meaning == "Muster");
        Assert.Contains(translations, t => t.Type == LanguageType.French && t.Meaning == "exemple");
    }

    [Fact]
    public async Task Command_accepts_missing_family_even_if_form_is_bypassed()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        var result = await env.Mediator.Send(new VocabularyBot.Application.Workflows.RegisterWord.RegisterWordCommand(10,
            [course.Id.ToString(), "word", "pronunciation", "معنی", " ", ""]));
        Assert.True(result.IsSuccessful);
        Assert.Equal("", (await env.Db.VocabularyEntries.SingleAsync()).Family);
    }

    [Fact]
    public async Task Translation_type_is_unique_per_word_and_stored_as_an_enum()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        var user = await env.Db.Users.SingleAsync(x => x.TelegramId == 10);
        var word = new VocabularyEntry { UserId = user.Id, CourseId = course.Id, EnglishWord = "example", NormalizedWord = "EXAMPLE", Pronunciation = "example", PersianMeaning = "معنی", Family = "family" };
        env.Db.VocabularyEntries.Add(word);
        env.Db.WordTranslations.Add(new WordTranslation { VocabularyEntryId = word.Id, Type = LanguageType.German, Meaning = "Beispiel" });
        await env.Db.SaveChangesAsync();
        env.Db.ChangeTracker.Clear();
        var saved = await env.Db.WordTranslations.SingleAsync();
        Assert.Equal(LanguageType.German, saved.Type);
        env.Db.WordTranslations.Add(new WordTranslation { VocabularyEntryId = word.Id, Type = LanguageType.German, Meaning = "Muster" });
        await Assert.ThrowsAsync<DbUpdateException>(() => env.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Menus_follow_registration_and_role_and_home_clears_the_draft()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start");
        var welcome = await env.Reply(10, BotMenu.Cancel);
        Assert.Contains(welcome.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Register);
        Assert.DoesNotContain(welcome.Keyboard.Rows.SelectMany(x => x), b => b.Text == BotMenu.Admin || b.Text == BotMenu.Word);
        await env.Send(10, BotMenu.Register); await env.Send(10, "100"); await JoinClass(env, 10);
        await env.Send(10, BotMenu.Word); await env.Send(10, "draft");
        var home = await env.Reply(10, BotMenu.Home);
        Assert.Contains(home.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Word);
        Assert.Empty(await env.Db.Conversations.ToListAsync());
        Assert.Empty(await env.Db.VocabularyEntries.ToListAsync());
        Assert.DoesNotContain("/admin", await env.Send(10, BotMenu.Help));
        await env.Send(1, "/start");
        var admin = await env.Reply(1, BotMenu.Cancel);
        Assert.Contains(admin.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Admin);
    }

    [Fact]
    public async Task Class_buttons_paginate_and_expired_buttons_do_not_modify_a_new_form()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        env.Db.Courses.AddRange(Enumerable.Range(1, 10).Select(i => new Course { University = $"University {i:00}", Year = 1405, Term = 1 }));
        await env.Db.SaveChangesAsync();
        await env.Send(10, "/start");
        var first = await env.Reply(10, "100");
        Assert.True(first.Keyboard!.Inline);
        Assert.Equal(8, first.Keyboard.Rows.SelectMany(x => x).Count(b => b.Text.StartsWith("University")));
        var next = first.Keyboard.Rows.SelectMany(x => x).Single(b => b.CallbackData!.StartsWith("page:"));
        var last = await env.Reply(10, next.CallbackData!);
        Assert.Equal(2, last.Keyboard!.Rows.SelectMany(x => x).Count(b => b.Text.StartsWith("University")));
        var selected = last.Keyboard.Rows[0][0];
        Assert.Contains("عضویت", await env.Send(10, selected.CallbackData!));
        Assert.Single(await env.Db.CourseMemberships.ToListAsync());
        await env.Send(10, BotMenu.Word);
        Assert.Contains("دیگر فعال نیست", await env.Send(10, selected.CallbackData!));
        Assert.Equal(1, (await env.Db.Conversations.SingleAsync()).Step);
        await env.Send(10, BotMenu.Join);
        Assert.Contains("دیگر فعال نیست", await env.Send(10, selected.CallbackData!));
        var oldCancel = first.Keyboard.Rows.SelectMany(x => x).Single(b => b.Text == BotMenu.Cancel);
        Assert.Contains("دیگر فعال نیست", await env.Send(10, oldCancel.CallbackData!));
        Assert.Single(await env.Db.Conversations.ToListAsync());
        foreach (var button in first.Keyboard.Rows.SelectMany(x => x))
            Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(button.CallbackData!), 1, 64);
    }

    [Fact]
    public async Task Back_allows_correction_and_buttons_complete_word_registration()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100"); await JoinClass(env, 10);
        var start = await env.Reply(10, BotMenu.Word);
        Assert.DoesNotContain(start.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Back || b.Text == BotMenu.Done);
        await env.Send(10, "incorrect");
        Assert.Contains("مرحله ۱ از ۶", await env.Send(10, BotMenu.Back));
        await env.Send(10, "correct"); await env.Send(10, "pronunciation");
        await env.Send(10, "معنی");
        var synonyms = await env.Reply(10, "family");
        Assert.Contains(synonyms.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Skip);
        var translations = await env.Reply(10, BotMenu.Skip);
        Assert.Contains(translations.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Done);
        await env.Send(10, "آلمانی: Beispiel");
        Assert.Contains("مترادف و ترجمه‌ها", await env.Send(10, BotMenu.Back));
        await env.Send(10, "synonym");
        var done = await env.Reply(10, BotMenu.Done);
        Assert.Contains("ثبت شد", done.Text);
        Assert.Contains(done.Keyboard!.Rows.SelectMany(x => x), b => b.Text == BotMenu.Word);
        var word = await env.Db.VocabularyEntries.SingleAsync();
        Assert.Equal("correct", word.EnglishWord);
        Assert.Equal("synonym", word.Synonyms);
        Assert.Empty(await env.Db.WordTranslations.ToListAsync());
    }

    [Fact]
    public async Task Word_class_buttons_only_show_memberships_and_reject_a_forged_selection()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var first = await JoinClass(env, 10); await JoinClass(env, 10);
        var other = new Course { University = "Other", Year = 1405, Term = 1 };
        env.Db.Courses.Add(other); await env.Db.SaveChangesAsync();
        var reply = await env.Reply(10, BotMenu.Word);
        var buttons = reply.Keyboard!.Rows.SelectMany(x => x).ToList();
        Assert.DoesNotContain(buttons, b => b.CallbackData!.EndsWith(other.Id.ToString("N")));
        var selection = buttons.Single(b => b.CallbackData!.EndsWith(first.Id.ToString("N"))).CallbackData!;
        Assert.Contains("عضو آن نیستید", await env.Send(10, selection.Replace(first.Id.ToString("N"), other.Id.ToString("N"))));
        Assert.Equal(0, (await env.Db.Conversations.SingleAsync()).Step);
        Assert.Contains("مرحله ۱ از ۶", await env.Send(10, selection));
    }

    [Fact]
    public async Task Back_corrects_course_year_without_losing_university()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(1, "/start"); await env.Send(1, "100");
        await env.Send(1, BotMenu.Course); await env.Send(1, "University"); await env.Send(1, "۱۴۰۴");
        Assert.Contains("مرحله ۲ از ۳", await env.Send(1, BotMenu.Back));
        await env.Send(1, "۱۴۰۵");
        Assert.Contains("ثبت شد", await env.Send(1, "۱"));
        var course = await env.Db.Courses.SingleAsync();
        Assert.Equal("University", course.University);
        Assert.Equal(1405, course.Year);
    }

    [Fact]
    public async Task Command_rejects_unknown_translation_type_without_partial_save()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        var result = await env.Mediator.Send(new VocabularyBot.Application.Workflows.RegisterWord.RegisterWordCommand(10,
            [course.Id.ToString(), "example", "pronunciation", "معنی", "family", "", "ژاپنی: 例"]));
        Assert.False(result.IsSuccessful);
        Assert.Empty(await env.Db.VocabularyEntries.ToListAsync());
        Assert.Empty(await env.Db.WordTranslations.ToListAsync());
    }

    [Fact]
    public async Task Admin_can_assign_unstarted_user_and_browse_class_words()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(1, "/start");
        await env.Send(1, "/admin");
        Assert.Contains("ادمین", await env.Send(1, "۲۲۲"));
        Assert.True((await env.Db.Users.SingleAsync(u => u.TelegramId == 222)).IsAdmin);
        var admin = await env.Reply(222, "/start");
        Assert.Contains(admin.Keyboard!.Rows.SelectMany(r => r), b => b.Text == BotMenu.Browse);
        await env.Send(10, "/start"); await env.Send(10, "100");
        await env.Send(11, "/start"); await env.Send(11, "101");
        var course = await JoinClass(env, 10, 11);
        var users = await env.Db.Users.Where(u => u.TelegramId == 10 || u.TelegramId == 11).OrderBy(u => u.TelegramId).ToListAsync();
        env.Db.VocabularyEntries.AddRange(
            new VocabularyEntry { CourseId = course.Id, UserId = users[0].Id, EnglishWord = "alpha", NormalizedWord = "ALPHA", Pronunciation = "a", PersianMeaning = "الف" },
            new VocabularyEntry { CourseId = course.Id, UserId = users[1].Id, EnglishWord = "beta", NormalizedWord = "BETA", Pronunciation = "b", PersianMeaning = "ب" });
        await env.Db.SaveChangesAsync();
        Assert.Contains("مخصوص ادمین", await env.Send(10, "/browse"));
        var classes = await env.Reply(222, BotMenu.Browse);
        var classButton = classes.Keyboard!.Rows.SelectMany(r => r).Single(b => b.CallbackData!.EndsWith(course.Id.ToString("N")));
        var students = await env.Reply(222, classButton.CallbackData!);
        var student = students.Keyboard!.Rows.SelectMany(r => r).Single(b => b.Text == "100");
        var studentWords = await env.Send(222, student.CallbackData!);
        Assert.Contains("alpha", studentWords); Assert.DoesNotContain("beta", studentWords);
        var all = students.Keyboard.Rows.SelectMany(r => r).Single(b => b.Text.Contains("همه واژه"));
        var allWords = await env.Send(222, all.CallbackData!);
        Assert.Contains("alpha", allWords); Assert.Contains("beta", allWords);
        Assert.Contains("پیدا نشد", await env.Send(222, student.CallbackData!.Replace(users[0].Id.ToString("N"), Guid.NewGuid().ToString("N"))));
        Assert.All(students.Keyboard.Rows.SelectMany(r => r), b => Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(b.CallbackData!), 1, 64));
    }

    [Fact]
    public async Task My_classes_are_selectable_and_limited_to_memberships()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        await env.Send(11, "/start"); await env.Send(11, "101");
        var memberCourse = await JoinClass(env, 10, 11);
        var otherCourse = await JoinClass(env, 11);
        var owner = await env.Db.Users.SingleAsync(u => u.TelegramId == 11);
        env.Db.VocabularyEntries.Add(new VocabularyEntry { CourseId = memberCourse.Id, UserId = owner.Id, EnglishWord = "alpha", NormalizedWord = "ALPHA", Pronunciation = "a", PersianMeaning = "الف" });
        await env.Db.SaveChangesAsync();
        var classes = await env.Reply(10, BotMenu.Classes);
        Assert.True(classes.Keyboard!.Inline);
        var buttons = classes.Keyboard.Rows.SelectMany(r => r).ToArray();
        var courseButton = buttons.Single(b => b.CallbackData!.EndsWith(memberCourse.Id.ToString("N")));
        Assert.DoesNotContain(buttons, b => b.CallbackData!.EndsWith(otherCourse.Id.ToString("N")));
        Assert.Contains("معتبر نیست", await env.Send(10, courseButton.CallbackData!.Replace(memberCourse.Id.ToString("N"), otherCourse.Id.ToString("N"))));
        var students = await env.Reply(10, courseButton.CallbackData!);
        var studentButton = students.Keyboard!.Rows.SelectMany(r => r).Single(b => b.Text == "101");
        Assert.Contains("alpha", await env.Send(10, studentButton.CallbackData!));
        var all = students.Keyboard.Rows.SelectMany(r => r).Single(b => b.Text.Contains("همه واژه"));
        Assert.Contains("alpha", await env.Send(10, all.CallbackData!));
    }

    [Fact]
    public void Polling_accepts_class_browsing_callbacks()
    {
        Assert.True(PollingWorker.IsSupportedCallback("browse:b12345678:course:00000000000000000000000000000000"));
        Assert.True(PollingWorker.IsSupportedCallback("class:j12345678:cancel"));
        Assert.False(PollingWorker.IsSupportedCallback("unknown:value"));
    }

}
