using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Tests;

public sealed class GenericRepositoryTests
{
    [Fact]
    public async Task Repository_preserves_tracking_and_scoped_lifetime()
    {
        var path = Path.GetTempFileName();
        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<BotDb>(options => options.UseSqlite($"Data Source={path};Pooling=False"));
            services.AddInfrastructure();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDb>();
            await db.Database.MigrateAsync();
            var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
            Assert.Same(repository, scope.ServiceProvider.GetRequiredService<IGenericRepository>());
            repository.Create(new Course { University = "University", Year = 404, Term = 1 });
            await repository.SaveChangesAsync();
            var course = await repository.Query<Course>().SingleAsync();
            var detached = await repository.FindAsync<Course>(course.Id, false);
            Assert.NotSame(course, detached);
            detached!.University = "Changed";
            await repository.SaveChangesAsync();
            Assert.Equal("University", (await repository.FindAsync<Course>(course.Id))!.University);
            repository.ClearChanges();
            repository.Update(detached);
            await repository.SaveChangesAsync();
            Assert.Equal("Changed", repository.Find<Course>(course.Id, false)!.University);
            repository.Delete(detached);
            await repository.SaveChangesAsync();
            Assert.Null(await repository.FindAsync<Course>(course.Id));
        }
        finally
        {
            File.Delete(path);
        }
    }
}