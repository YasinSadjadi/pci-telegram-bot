using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VocabularyBot.Application.Conversations;

public sealed class PollingWorker(IServiceScopeFactory scopes, TelegramClient telegram, ILogger<PollingWorker> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        long offset = 0;
        while (!ct.IsCancellationRequested)
            try
            {
                var updates = await telegram.Call("getUpdates",
                    new { offset, timeout = 30, allowed_updates = new[] { "message" } }, ct);
                foreach (var update in updates.EnumerateArray())
                {
                    var updateId = update.GetProperty("update_id").GetInt64();
                    if (update.TryGetProperty("message", out var msg) && msg.TryGetProperty("text", out var text) &&
                        msg.TryGetProperty("from", out var from) &&
                        msg.GetProperty("chat").GetProperty("type").GetString() == "private")
                    {
                        var id = from.GetProperty("id").GetInt64();
                        var username = from.TryGetProperty("username", out var un) ? un.GetString() : null;
                        var name = from.TryGetProperty("first_name", out var n) ? n.GetString() ?? "" : "";
                        using var scope = scopes.CreateScope();
                        var answer = await scope.ServiceProvider.GetRequiredService<IMediator>()
                            .Send(new ProcessMessageCommand(id, username, name, text.GetString() ?? ""), ct);
                        await telegram.Send(id, answer, ct);
                    }

                    offset = updateId + 1;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogWarning("Polling failed: {Error}", ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
    }
}