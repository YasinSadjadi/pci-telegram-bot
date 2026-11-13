using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VocabularyBot.Application;
using VocabularyBot.Infrastructure;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the BOT_TOKEN environment variable.");
    return;
}

var folder = Environment.GetEnvironmentVariable("BOT_DATA_DIR") ??
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VocabularyBot");
Directory.CreateDirectory(folder);
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={Path.Combine(folder, "vocabulary.db")}"));
builder.Services.AddInfrastructure();
var configuredAdmin = Environment.GetEnvironmentVariable("BOT_ADMIN_ID");
if (!string.IsNullOrWhiteSpace(configuredAdmin) && (!long.TryParse(configuredAdmin, out var parsedAdmin) || parsedAdmin <= 0))
{
    Console.Error.WriteLine("BOT_ADMIN_ID must be a positive Telegram numeric ID.");
    return;
}
builder.Services.AddApplication(long.TryParse(configuredAdmin, out var adminId) ? adminId : null);
builder.Services.AddSingleton(new TelegramClient(token));
builder.Services.AddHostedService<PollingWorker>();
using var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BotDb>().Database.MigrateAsync();
}

await host.RunAsync();