using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VocabularyBot.Application.Reminders;

public sealed class ReminderWorker(IServiceScopeFactory scopes, TelegramClient telegram, ILogger<ReminderWorker> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Process(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogWarning("Reminder processing failed: {Error}", ex.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task Process(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ReminderService>();
        await service.Process(DateTime.UtcNow, telegram.Send, ct);
    }
}