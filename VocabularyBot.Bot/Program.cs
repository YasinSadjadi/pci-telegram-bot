using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VocabularyBot.Application;
using VocabularyBot.Infrastructure;

var settings = new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
};
#if DEBUG
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")) &&
    !args.Any(arg => arg.Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
                     arg.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase)))
    settings.EnvironmentName = Environments.Development;
#endif
var builder = Host.CreateApplicationBuilder(settings);
var token = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? builder.Configuration["Bot:Token"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set BOT_TOKEN or the Bot:Token configuration value.");
    return;
}

var folder = Environment.GetEnvironmentVariable("BOT_DATA_DIR") ?? builder.Configuration["Bot:DataDirectory"] ??
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VocabularyBot");
Directory.CreateDirectory(folder);
builder.Services.AddDbContext<BotDb>(o => o.UseSqlite($"Data Source={Path.Combine(folder, "vocabulary.db")}"));
builder.Services.AddInfrastructure();
var configuredAdmin = Environment.GetEnvironmentVariable("BOT_ADMIN_ID") ?? builder.Configuration["Bot:AdminId"];
if (!string.IsNullOrWhiteSpace(configuredAdmin) &&
    (!long.TryParse(configuredAdmin, out var parsedAdmin) || parsedAdmin <= 0))
{
    Console.Error.WriteLine("BOT_ADMIN_ID or Bot:AdminId must be a positive Telegram numeric ID.");
    return;
}

var adminUsername = Environment.GetEnvironmentVariable("BOT_ADMIN_USERNAME") ??
                    builder.Configuration["Bot:AdminUsername"];
builder.Services.AddApplication(long.TryParse(configuredAdmin, out var adminId) ? adminId : null, adminUsername);
builder.Services.AddSingleton(new TelegramClient(token));
builder.Services.AddHostedService<PollingWorker>();
builder.Services.AddHostedService<WeeklyBackupWorker>();
using var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BotDb>().Database.MigrateAsync();
}

await host.RunAsync();
