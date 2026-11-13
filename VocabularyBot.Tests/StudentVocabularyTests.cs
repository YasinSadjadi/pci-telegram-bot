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
        public TestEnvironment()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={path};Pooling=False"));
            services.AddInfrastructure();
            services.AddApplication(1);
            provider = services.BuildServiceProvider();
            scope = provider.CreateAsyncScope();
        }
        public Task Initialize() => Db.Database.MigrateAsync();
        public Task<string> Send(long id, string text) => Mediator.Send(new ProcessMessageCommand(id, "user" + id, "Student", text));
        public async ValueTask DisposeAsync() { await scope.DisposeAsync(); await provider.DisposeAsync(); File.Delete(path); }
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
        Assert.Contains("اجباری", await env.Send(10, "/skip"));
        await env.Send(10, "consistent, consistency");
        await env.Send(10, "/skip");
        Assert.Contains("ثبت شد", await env.Send(10, "/done"));
        var entry = await env.Db.VocabularyEntries.SingleAsync();
        Assert.Equal("Eventual Consistency", entry.EnglishWord); Assert.Equal("/ɪˈventʃuəl/", entry.Pronunciation);
        Assert.Equal("consistent, consistency", entry.Family); Assert.Null(entry.Synonyms);
        Assert.Null(entry.GermanTranslation); Assert.Null(entry.FrenchTranslation); Assert.Null(entry.SpanishTranslation); Assert.Null(entry.Translations);
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
            await env.Send(user, "اسپانیایی: ejemplo"); await env.Send(user, "ایتالیایی: esempio");
            return await env.Send(user, "/done");
        }
        Assert.Contains("ثبت شد", await Word(10, "  Example   Phrase "));
        await env.Send(11, "/word");
        Assert.Contains("قبلاً", await env.Send(11, "example phrase"));
        await env.Send(11, "/cancel");
        var word = await env.Db.VocabularyEntries.SingleAsync();
        Assert.Equal("family", word.Family); Assert.Equal("synonym", word.Synonyms);
        var translations = await env.Db.WordTranslations.Where(x => x.VocabularyEntryId == word.Id).ToListAsync();
        Assert.Equal(4, translations.Count);
        Assert.Contains(translations, x => x.Language == "آلمانی" && x.Meaning == "Beispiel");
        Assert.Contains(translations, x => x.Language == "ایتالیایی" && x.Meaning == "esempio");
        var list = await env.Send(10, "/words");
        Assert.Contains("خانواده واژه: family", list);
        Assert.Contains("آلمانی: Beispiel", list);
        Assert.Contains("فرانسوی: exemple", list);
        Assert.Contains("اسپانیایی: ejemplo", list);
        Assert.DoesNotContain("Example", await env.Send(11, "/words"));
    }

    [Fact]
    public async Task Unregistered_users_cannot_enter_word_and_legacy_group_commands_are_unavailable()
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
        env.Db.VocabularyEntries.Add(new VocabularyEntry { UserId = users[0].Id, EnglishWord = "TEST", NormalizedWord = "TEST", Pronunciation = "test", PersianMeaning = "معنی" });
        await env.Db.SaveChangesAsync();
        env.Db.VocabularyEntries.Add(new VocabularyEntry { UserId = users[1].Id, EnglishWord = "test", NormalizedWord = "TEST", Pronunciation = "test", PersianMeaning = "معنی" });
        await Assert.ThrowsAsync<DbUpdateException>(() => env.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Legacy_vocabulary_also_blocks_duplicate_registration()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        env.Db.LegacyVocabularyTitles.Add(new LegacyVocabularyTitle { NormalizedTitle = "EXAMPLE" });
        await env.Db.SaveChangesAsync();
        var currentCourse = await JoinClass(env, 10);
        await env.Send(10, "/word");
        Assert.Contains("قبلاً", await env.Send(10, "example"));
        Assert.Empty(await env.Db.VocabularyEntries.ToListAsync());
    }

    [Fact]
    public async Task Student_selects_a_class_during_registration_and_can_join_another_class()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var first = new Course { University = "First", Year = 404, Term = 1 };
        var second = new Course { University = "Second", Year = 404, Term = 2 };
        env.Db.Courses.AddRange(first, second); await env.Db.SaveChangesAsync();
        await env.Send(10, "/start");
        var list = await env.Send(10, "100");
        Assert.Contains(first.Id.ToString(), list);
        Assert.Contains(second.Id.ToString(), list);
        Assert.Contains("پیدا نشد", await env.Send(10, "wrong"));
        Assert.Contains("عضویت", await env.Send(10, first.Id.ToString()));
        Assert.Contains(first.Id.ToString(), await env.Send(10, "/classes"));
        Assert.DoesNotContain(second.Id.ToString(), await env.Send(10, "/classes"));
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
        Assert.Contains("ساخته شد", await env.Send(1, "۱"));
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
        Assert.Contains(course.Id.ToString(), await env.Send(10, "/join"));
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
        Assert.Contains(course.Id.ToString(), await env.Send(10, "1234"));
        Assert.Contains("عضویت", await env.Send(10, course.Id.ToString()));
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
    public async Task Old_in_progress_word_form_is_not_silently_reinterpreted()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var user = await env.Db.Users.SingleAsync();
        var state = await env.Db.Conversations.SingleAsync(x => x.UserId == user.Id);
        state.Flow = "word"; state.Data = "[\"old draft\"]";
        await env.Db.SaveChangesAsync();
        var response = await env.Send(10, "value");
        Assert.Contains("/word", response);
        Assert.Equal("[\"old draft\"]", (await env.Db.Conversations.SingleAsync(x => x.UserId == user.Id)).Data);
    }

    [Fact]
    public async Task Translations_accept_arbitrary_languages_and_allow_corrections_without_losing_draft()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100"); await JoinClass(env, 10);
        await env.Send(10, "/word"); await env.Send(10, "example"); await env.Send(10, "pronunciation");
        await env.Send(10, "معنی"); await env.Send(10, "family"); await env.Send(10, "/skip");
        Assert.Contains("قالب", await env.Send(10, "ترجمه بدون زبان"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "ژاپنی: 例"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "ژاپنی: 新"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "Portuguese: exemplo"));
        Assert.Contains("به فرم اضافه شد", await env.Send(10, "portuguese: outro"));
        Assert.Contains("ثبت شد", await env.Send(10, "/done"));
        var translations = await env.Db.WordTranslations.ToListAsync();
        Assert.Equal(2, translations.Count);
        Assert.Contains(translations, t => t.Language == "ژاپنی" && t.Meaning == "新");
        Assert.Contains(translations, t => t.Language == "portuguese" && t.Meaning == "outro");
    }

    [Fact]
    public async Task Command_rejects_missing_family_even_if_form_is_bypassed()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Send(10, "/start"); await env.Send(10, "100");
        var course = await JoinClass(env, 10);
        var result = await env.Mediator.Send(new VocabularyBot.Application.Workflows.RegisterWord.RegisterWordCommand(10,
            [course.Id.ToString(), "word", "pronunciation", "معنی", " ", ""]));
        Assert.False(result.IsSuccessful);
        Assert.Contains("خانواده", result.Message);
        Assert.Empty(await env.Db.VocabularyEntries.ToListAsync());
    }

}
