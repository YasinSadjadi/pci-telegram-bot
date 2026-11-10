using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application;
using VocabularyBot.Application.Queries.ListCourses;
using VocabularyBot.Application.Queries.ListUsers;
using VocabularyBot.Application.Workflows.AssignAdmin;
using VocabularyBot.Application.Workflows.CreateCourse;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;

namespace VocabularyBot.Tests;

public sealed class CqrsTests
{
    [Fact]
    public async Task Commands_persist_and_queries_enforce_access_via_mediator()
    {
        var path = Path.GetTempFileName();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={path};Pooling=False"));
            services.AddInfrastructure();
            services.AddApplication(1);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDb>();
            await db.Database.MigrateAsync();
            db.Users.AddRange(new User { TelegramId = 1, Name = "Admin", IsAdmin = true },
                new User { TelegramId = 2, Name = "Student", Username = "student" });
            await db.SaveChangesAsync();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.False((await mediator.Send(new CreateCourseCommand(2, ["University", "404", "1"]))).IsSuccessful);
            Assert.True((await mediator.Send(new CreateCourseCommand(1, ["University", "404", "1"]))).IsSuccessful);
            Assert.Contains("University", await mediator.Send(new ListCoursesQuery(2)));
            Assert.Equal("دسترسی ندارید.", await mediator.Send(new ListUsersQuery(2)));
            Assert.True((await mediator.Send(new AssignAdminCommand(1, ["student"]))).IsSuccessful);
            db.ChangeTracker.Clear();
            Assert.True((await db.Users.SingleAsync(x => x.TelegramId == 2))!.IsAdmin);
        }
        finally
        {
            File.Delete(path);
        }
    }
}