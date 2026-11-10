using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application;
using VocabularyBot.Application.Reminders;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;
namespace VocabularyBot.Tests;

public class ReminderTests
{
    [Fact]
    public async Task Reminder_retries_and_keeps_telegram_id()
    {
        var path = Path.GetTempFileName(); try
        {
            var services = new ServiceCollection(); services.AddLogging(); services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={path};Pooling=False")); services.AddInfrastructure(); services.AddApplication(1);
            await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<BotDb>(); await db.Database.MigrateAsync();
            var now = DateTime.UtcNow; var u = new User { TelegramId = 7, Name = "A" }; var c = new Course { University = "U", Year = 404, Term = 1 }; var g = new StudyGroup { CourseId = c.Id, Number = 1 }; var w = new StudyWeek { CourseId = c.Id, Label = "week", StartUtc = now.AddDays(-7), EndUtc = now.AddDays(-1), ReminderUtc = now.AddDays(-2) };
            db.AddRange(u, c, g, w, new Membership { GroupId = g.Id, UserId = u.Id }); await db.SaveChangesAsync(); var service = scope.ServiceProvider.GetRequiredService<ReminderService>(); var attempts = 0;
            Task Send(long id, string message, CancellationToken ct) { Assert.Equal(7, id); attempts++; if (attempts == 1) throw new Exception(); return Task.CompletedTask; }
            await service.Process(now, Send, default); await service.Process(now, Send, default); await service.Process(now, Send, default); Assert.Equal(2, attempts); Assert.Equal(1, await db.Reminders.CountAsync());
        }
        finally { File.Delete(path); }
    }
}
