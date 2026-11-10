using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using VocabularyBot.Application;
using VocabularyBot.Application.Conversations;
using VocabularyBot.Application.Reminders;
using VocabularyBot.Application.Workflows.CreateWeek;
using VocabularyBot.Application.Workflows.RegisterWord;
using VocabularyBot.Application.Queries.ListGroups;
using VocabularyBot.Application.Queries.ListWeeks;
using VocabularyBot.Application.Queries.GetStatus;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;

namespace VocabularyBot.Tests;

public sealed class IntegrationScenarioTests
{
    private sealed class TestEnvironment : IAsyncDisposable
    {
        private readonly string path = Path.GetTempFileName();
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;
        public BotDb Db => scope.ServiceProvider.GetRequiredService<BotDb>();
        public IMediator Mediator => scope.ServiceProvider.GetRequiredService<IMediator>();
        public BotService Bot => scope.ServiceProvider.GetRequiredService<BotService>();
        public ReminderService Reminders => scope.ServiceProvider.GetRequiredService<ReminderService>();
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
        public async Task Initialize() => await Db.Database.MigrateAsync();
        public AsyncServiceScope NewScope() => provider.CreateAsyncScope();
        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Classes_groups_and_weeks_are_filtered_by_membership_and_admin_reports_are_protected()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var admin = new User { TelegramId = 1, Name = "Admin", IsAdmin = true };
        var member = new User { TelegramId = 2, Name = "Member" };
        var first = new Course { University = "First University", Year = 404, Term = 1 };
        var second = new Course { University = "Second University", Year = 404, Term = 1 };
        var group1 = new StudyGroup { CourseId = first.Id, Number = 1 };
        var group2 = new StudyGroup { CourseId = second.Id, Number = 2 };
        var week1 = new StudyWeek { CourseId = first.Id, Label = "First week", StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddDays(1) };
        var week2 = new StudyWeek { CourseId = second.Id, Label = "Second week", StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddDays(1) };
        env.Db.AddRange(admin, member, first, second, group1, group2, week1, week2, new Membership { GroupId = group1.Id, UserId = member.Id });
        await env.Db.SaveChangesAsync();
        var groups = await env.Mediator.Send(new ListGroupsQuery(2));
        Assert.Contains(group1.Id.ToString(), groups); Assert.DoesNotContain(group2.Id.ToString(), groups);
        var weeks = await env.Mediator.Send(new ListWeeksQuery(2));
        Assert.Contains("First week", weeks); Assert.DoesNotContain("Second week", weeks);
        Assert.Contains("Second week", await env.Mediator.Send(new ListWeeksQuery(1)));
        Assert.Equal("دسترسی ندارید.", await env.Mediator.Send(new GetStatusQuery(2)));
        Assert.Contains("Member", await env.Mediator.Send(new GetStatusQuery(1)));
    }

    [Fact]
    public async Task Week_validation_rejects_overlap_and_bad_reminder_but_allows_adjacent_weeks()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var admin = new User { TelegramId = 1, Name = "Admin", IsAdmin = true };
        var course = new Course { University = "University", Year = 404, Term = 1 };
        env.Db.AddRange(admin, course); await env.Db.SaveChangesAsync();
        string id = course.Id.ToString();
        var first = await env.Mediator.Send(new CreateWeekCommand(1, [id, "First", "2026-01-01 09:00", "2026-01-07 18:00", "17:00"]));
        var overlap = await env.Mediator.Send(new CreateWeekCommand(1, [id, "Overlap", "2026-01-07 17:59", "2026-01-10 18:00", "17:00"]));
        var badTime = await env.Mediator.Send(new CreateWeekCommand(1, [id, "Bad", "2026-01-07 18:00", "2026-01-10 12:00", "13:00"]));
        var adjacent = await env.Mediator.Send(new CreateWeekCommand(1, [id, "Second", "2026-01-07 18:00", "2026-01-10 18:00", "17:00"]));
        Assert.True(first.IsSuccessful); Assert.False(overlap.IsSuccessful); Assert.False(badTime.IsSuccessful); Assert.True(adjacent.IsSuccessful);
        Assert.Equal(2, await env.Db.Weeks.CountAsync());
    }

    [Fact]
    public async Task Word_registration_requires_membership_and_active_week_and_duplicate_is_scoped_to_course()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var user = new User { TelegramId = 2, Name = "Member" };
        var other = new User { TelegramId = 3, Name = "Outsider" };
        var c1 = new Course { University = "A", Year = 404, Term = 1 };
        var c2 = new Course { University = "B", Year = 404, Term = 1 };
        var g1 = new StudyGroup { CourseId = c1.Id, Number = 1 }; var g2 = new StudyGroup { CourseId = c2.Id, Number = 1 };
        var active1 = new StudyWeek { CourseId = c1.Id, Label = "Active A", StartUtc = DateTime.UtcNow.AddHours(-1), EndUtc = DateTime.UtcNow.AddHours(1) };
        var active2 = new StudyWeek { CourseId = c2.Id, Label = "Active B", StartUtc = DateTime.UtcNow.AddHours(-1), EndUtc = DateTime.UtcNow.AddHours(1) };
        var ended = new StudyWeek { CourseId = c1.Id, Label = "Ended", StartUtc = DateTime.UtcNow.AddDays(-2), EndUtc = DateTime.UtcNow.AddDays(-1) };
        env.Db.AddRange(user, other, c1, c2, g1, g2, active1, active2, ended, new Membership { UserId = user.Id, GroupId = g1.Id }, new Membership { UserId = user.Id, GroupId = g2.Id }); await env.Db.SaveChangesAsync();
        static string[] Values(Guid g, Guid w, string title) => [g.ToString(), w.ToString(), title, "معنی", "definition", "example", "related", "source"];
        Assert.False((await env.Mediator.Send(new RegisterWordCommand(3, Values(g1.Id, active1.Id, "test")))).IsSuccessful);
        Assert.False((await env.Mediator.Send(new RegisterWordCommand(2, Values(g1.Id, ended.Id, "test")))).IsSuccessful);
        Assert.False((await env.Mediator.Send(new RegisterWordCommand(2, Values(g1.Id, active2.Id, "test")))).IsSuccessful);
        Assert.True((await env.Mediator.Send(new RegisterWordCommand(2, Values(g1.Id, active1.Id, "Eventual  Consistency")))).IsSuccessful);
        Assert.False((await env.Mediator.Send(new RegisterWordCommand(2, Values(g1.Id, active1.Id, "eventual consistency")))).IsSuccessful);
        Assert.True((await env.Mediator.Send(new RegisterWordCommand(2, Values(g2.Id, active2.Id, "eventual consistency")))).IsSuccessful);
        Assert.Equal(2, await env.Db.Words.CountAsync());
    }

    [Fact]
    public async Task Reminder_only_sends_to_members_below_five_and_does_not_repeat_after_restart()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var now = DateTime.UtcNow; var low = new User { TelegramId = 2, Name = "Low" }; var full = new User { TelegramId = 3, Name = "Full" };
        var c = new Course { University = "U", Year = 404, Term = 1 }; var group = new StudyGroup { CourseId = c.Id, Number = 1 };
        var week = new StudyWeek { CourseId = c.Id, Label = "Week", StartUtc = now.AddDays(-2), EndUtc = now.AddDays(1), ReminderUtc = now.AddHours(-1) };
        env.Db.AddRange(low, full, c, group, week, new Membership { GroupId = group.Id, UserId = low.Id }, new Membership { GroupId = group.Id, UserId = full.Id });
        for (int i = 0; i < 5; i++) env.Db.Words.Add(new Word { CourseId = c.Id, GroupId = group.Id, WeekId = week.Id, UserId = full.Id, Title = $"word{i}", NormalizedTitle = $"WORD{i}" });
        await env.Db.SaveChangesAsync(); var sent = new List<long>();
        Task Send(long id, string _, CancellationToken ct) { sent.Add(id); return Task.CompletedTask; }
        await env.Reminders.Process(now, Send, default);
        await using var restartedScope = env.NewScope();
        await restartedScope.ServiceProvider.GetRequiredService<ReminderService>().Process(now.AddHours(1), Send, default);
        Assert.Equal(new long[] { 2 }, sent); Assert.Single(await env.Db.Reminders.ToListAsync());
    }

    [Fact]
    public async Task Conversation_cancel_clears_partial_state_and_rejects_unregistered_users()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        Assert.Contains("ابتدا", await env.Bot.Handle(2, "student", "Student", "/word"));
        await env.Bot.Handle(1, "admin", "Admin", "/start");
        await env.Bot.Handle(1, "admin", "Admin", "/course");
        await env.Bot.Handle(1, "admin", "Admin", "University");
        Assert.Single(await env.Db.Conversations.ToListAsync());
        Assert.Contains("لغو", await env.Bot.Handle(1, "admin", "Admin", "/cancel"));
        Assert.Empty(await env.Db.Conversations.ToListAsync());
        Assert.Empty(await env.Db.Courses.ToListAsync());
    }
    [Fact]
    public async Task Database_unique_constraint_blocks_duplicate_even_from_independent_contexts()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        var user = new User { TelegramId = 2, Name = "Member" };
        var course = new Course { University = "U", Year = 404, Term = 1 };
        var group = new StudyGroup { CourseId = course.Id, Number = 1 };
        var week = new StudyWeek { CourseId = course.Id, Label = "Week", StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddDays(1) };
        env.Db.AddRange(user, course, group, week); await env.Db.SaveChangesAsync();
        await using var scope1 = env.NewScope(); await using var scope2 = env.NewScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<BotDb>(); var db2 = scope2.ServiceProvider.GetRequiredService<BotDb>();
        Word CreateWord() => new() { CourseId = course.Id, GroupId = group.Id, WeekId = week.Id, UserId = user.Id, Title = "Example", NormalizedTitle = "EXAMPLE" };
        db1.Words.Add(CreateWord()); db2.Words.Add(CreateWord());
        await db1.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        Assert.Equal(1, await env.Db.Words.CountAsync());
    }

    [Fact]
    public async Task User_registration_is_idempotent_and_admin_cannot_be_assigned_by_an_ordinary_user()
    {
        await using var env = new TestEnvironment(); await env.Initialize();
        await env.Bot.Handle(2, "first", "Member", "/start");
        await env.Bot.Handle(2, "updated", "Updated", "/start");
        Assert.Single(await env.Db.Users.ToListAsync());
        Assert.Equal("updated", (await env.Db.Users.SingleAsync()).Username);
        Assert.Contains("دسترسی", await env.Bot.Handle(2, "updated", "Updated", "/admin"));
        Assert.False((await env.Db.Users.SingleAsync()).IsAdmin);
    }

    [Fact]
    public async Task Every_telegram_command_and_each_workflow_executes_through_mediator()
    {
        await using var env = new TestEnvironment();
        await env.Initialize();
        async Task<string> Send(long id, string text, string? username = null) =>
            await env.Mediator.Send(new ProcessMessageCommand(id, username, id == 1 ? "Admin" : "Student", text));
        async Task<string> Form(long id, string command, params string[] values)
        {
            var response = await Send(id, command);
            foreach (var value in values) response = await Send(id, value);
            return response;
        }
        Assert.Contains("شناسه", await Send(1, "/start", "admin"));
        await Send(2, "/start", "student");
        Assert.Contains("دستورها", await Send(1, "/help"));
        Assert.Contains("دستورها", await Send(1, "/menu"));
        Assert.Contains("ناشناخته", await Send(1, "/not_a_command"));
        Assert.Contains("ایجاد شد", await Form(1, "/course", "University", "404", "1"));
        var course = await env.Db.Courses.SingleAsync();
        Assert.Contains(course.Id.ToString(), await Send(1, "/classes"));
        Assert.Contains("ایجاد شد", await Form(1, "/group", course.Id.ToString(), "1"));
        var group = await env.Db.Groups.SingleAsync();
        Assert.Contains(group.Id.ToString(), await Send(1, "/groups"));
        Assert.Contains("اضافه شد", await Form(1, "/member", group.Id.ToString(), "student"));
        Assert.Contains("تعیین شد", await Form(1, "/leader", group.Id.ToString(), "student"));
        var student = await env.Db.Users.SingleAsync(x => x.TelegramId == 2);
        Assert.Equal(student.Id, (await env.Db.Groups.SingleAsync()).LeaderId);
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var start = localNow.Date.AddDays(-1).AddHours(9).ToString("yyyy-MM-dd HH:mm");
        var end = localNow.Date.AddDays(1).AddHours(23).AddMinutes(59).ToString("yyyy-MM-dd HH:mm");
        Assert.Contains("ثبت شد", await Form(1, "/week", course.Id.ToString(), "Week", start, end, "23:00"));
        var week = await env.Db.Weeks.SingleAsync();
        Assert.Contains("Week", await Send(2, "/weeks"));
        Assert.Contains("ثبت شد", await Form(2, "/word", group.Id.ToString(), week.Id.ToString(), "Example", "معنی", "definition", "example", "related", "source"));
        Assert.Contains("1/5", await Send(1, "/status"));
        Assert.Contains("student", await Send(1, "/users"));
        Assert.Contains("ادمین", await Form(1, "/admin", "student"));
        Assert.True((await env.Db.Users.SingleAsync(x => x.TelegramId == 2)).IsAdmin);
        await Send(1, "/course");
        Assert.Contains("لغو", await Send(1, "/cancel"));
    }

}
