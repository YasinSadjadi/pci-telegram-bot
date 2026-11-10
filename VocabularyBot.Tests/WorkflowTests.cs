using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application;
using VocabularyBot.Application.Conversations;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;
namespace VocabularyBot.Tests;

public class WorkflowTests
{
    [Fact]
    public async Task Group_members_and_words_use_guid_identifiers()
    {
        var path = Path.GetTempFileName();
        try
        {
            var services = new ServiceCollection(); services.AddLogging(); services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={path};Pooling=False")); services.AddInfrastructure(); services.AddApplication(1);
            await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<BotDb>(); await db.Database.MigrateAsync();
            var bot = scope.ServiceProvider.GetRequiredService<BotService>(); await bot.Handle(1, "admin", "Admin", "/start"); await bot.Handle(2, "student", "Student", "/start");
            await Flow(bot, 1, "course", "University", "404", "1"); var course = await db.Courses.SingleAsync();
            await Flow(bot, 1, "group", course.Id.ToString(), "1"); var group = await db.Groups.SingleAsync();
            await Flow(bot, 1, "member", group.Id.ToString(), "student");
            var week = new StudyWeek { CourseId = course.Id, Label = "week", StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddDays(1), ReminderUtc = DateTime.UtcNow }; db.Weeks.Add(week); await db.SaveChangesAsync();
            Assert.Contains("ثبت شد", await Flow(bot, 2, "word", group.Id.ToString(), week.Id.ToString(), "Eventual   Consistency", "معنی", "def", "example", "related", "source"));
            Assert.Contains("قبلاً", await Flow(bot, 2, "word", group.Id.ToString(), week.Id.ToString(), "eventual consistency", "معنی", "def", "example", "related", "source"));
            Assert.Single(await db.Words.ToListAsync());
        }
        finally { File.Delete(path); }
    }
    static async Task<string> Flow(BotService bot, long id, string flow, params string[] values) { await bot.Handle(id, null, "Name", "/" + flow); string result = ""; foreach (var value in values) result = await bot.Handle(id, null, "Name", value); return result; }
}
