using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VocabularyBot.Application;
using VocabularyBot.Infrastructure;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token) ||
    !long.TryParse(Environment.GetEnvironmentVariable("BOT_ADMIN_ID"), out var adminId) || adminId <= 0)
{
    Console.Error.WriteLine("Set BOT_TOKEN and BOT_ADMIN_ID environment variables.");
    return;
}

var folder = Environment.GetEnvironmentVariable("BOT_DATA_DIR") ??
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VocabularyBot");
Directory.CreateDirectory(folder);
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={Path.Combine(folder, "vocabulary.db")}"));
builder.Services.AddInfrastructure();
builder.Services.AddApplication(adminId);
builder.Services.AddSingleton(new TelegramClient(token));
builder.Services.AddHostedService<PollingWorker>();
builder.Services.AddHostedService<ReminderWorker>();
using var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BotDb>().Database.MigrateAsync();
}

await host.RunAsync();