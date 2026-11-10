using MediatR;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Reminders;

public sealed class ReminderService(IMediator mediator, IGenericRepository repository)
{
    public async Task Process(DateTime now, Func<long, string, CancellationToken, Task> send, CancellationToken ct)
    {
        var due = await mediator.Send(new GetDueRemindersQuery(now), ct);
        foreach (var x in due)
        {
            if (x.Count >= 5) continue;
            try
            {
                await send(x.TelegramId,
                    $"یادآوری: برای {x.WeekLabel} در گروه {x.GroupId} تاکنون {x.Count} واژه از ۵ واژه ثبت کرده‌اید.",
                    ct);
                await mediator.Send(new MarkReminderSentCommand(x.WeekId, x.GroupId, x.UserId, now), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                repository.ClearChanges();
            }
        }
    }
}