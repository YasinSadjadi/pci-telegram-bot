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
                    new { offset, timeout = 30, allowed_updates = new[] { "message", "callback_query" } }, ct);
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
                        await Respond(id, username, name, text.GetString() ?? "", ct);
                    }
                    else if (update.TryGetProperty("callback_query", out var callback))
                    {
                        await telegram.Call("answerCallbackQuery", new { callback_query_id = callback.GetProperty("id").GetString() }, ct);
                        if (callback.TryGetProperty("message", out var message) &&
                            message.GetProperty("chat").GetProperty("type").GetString() == "private" &&
                            callback.TryGetProperty("data", out var data))
                        {
                            var actor = callback.GetProperty("from");
                            var id = actor.GetProperty("id").GetInt64();
                            if (message.GetProperty("chat").GetProperty("id").GetInt64() == id)
                            {
                                var input = data.GetString() ?? "";
                                if (input.StartsWith("class:") || input.StartsWith("page:"))
                                    await Respond(id,
                                        actor.TryGetProperty("username", out var username) ? username.GetString() : null,
                                        actor.TryGetProperty("first_name", out var name) ? name.GetString() ?? "" : "",
                                        input, ct);
                            }
                        }
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

    private async Task Respond(long id, string? username, string name, string input, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var answer = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new ProcessMessageCommand(id, username, name, input), ct);
        await telegram.Send(id, answer, ct);
    }
}
