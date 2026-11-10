using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VocabularyBot.Infrastructure;
namespace VocabularyBot.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task Legacy_data_and_relationships_survive_identifier_migration()
    {
        var path = Path.GetTempFileName(); try
        {
            await using var db = new BotDb(new DbContextOptionsBuilder<BotDb>().UseSqlite($"Data Source={path};Pooling=False").Options);
            await db.GetService<IMigrator>().MigrateAsync("20261110191612_Initial");
            await db.Database.ExecuteSqlRawAsync("""
INSERT INTO Users(Id,Username,Name,IsAdmin) VALUES (12345,'alice','Alice',1);
INSERT INTO Courses(Id,University,Year,Term) VALUES (10,'U',404,1);
INSERT INTO Groups(Id,CourseId,Number,LeaderId) VALUES (20,10,1,12345);
INSERT INTO Weeks(Id,CourseId,Label,StartUtc,EndUtc,ReminderUtc) VALUES (30,10,'Week','2026-01-01','2026-01-08','2026-01-07');
INSERT INTO Memberships(GroupId,UserId) VALUES (20,12345);
INSERT INTO Conversations(UserId,Flow,Step,Data) VALUES (12345,'word',0,'[]');
INSERT INTO Reminders(WeekId,GroupId,UserId,SentUtc) VALUES (30,20,12345,'2026-01-07');
INSERT INTO Words(Id,CourseId,GroupId,WeekId,UserId,Title,NormalizedTitle,Meaning,Definition,Example,Related,Source) VALUES (40,10,20,30,12345,'Example','EXAMPLE','معنی','def','example','related','source');
""");
            await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
            var user = await db.Users.SingleAsync(); var course = await db.Courses.SingleAsync(); var group = await db.Groups.SingleAsync(); var week = await db.Weeks.SingleAsync(); var word = await db.Words.SingleAsync(); var member = await db.Memberships.SingleAsync(); var reminder = await db.Reminders.SingleAsync(); var conversation = await db.Conversations.SingleAsync();
            Assert.Equal(12345, user.TelegramId); Assert.Equal(course.Id, group.CourseId); Assert.Equal(user.Id, group.LeaderId); Assert.Equal(group.Id, member.GroupId); Assert.Equal(user.Id, member.UserId); Assert.Equal(course.Id, week.CourseId); Assert.Equal((course.Id, group.Id, week.Id, user.Id), (word.CourseId, word.GroupId, word.WeekId, word.UserId)); Assert.Equal((week.Id, group.Id, user.Id), (reminder.WeekId, reminder.GroupId, reminder.UserId)); Assert.Equal(user.Id, conversation.UserId); db.ChangeTracker.Clear(); Assert.NotNull(await db.Users.FindAsync(user.Id)); Assert.NotNull(await db.Courses.FindAsync(course.Id));
        }
        finally { File.Delete(path); }
    }
}
