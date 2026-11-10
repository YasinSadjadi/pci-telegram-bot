using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Reminders;

public sealed class MarkReminderSentHandler(IGenericRepository repository)
    : ICommandHandler<MarkReminderSentCommand, bool>
{
    public async Task<bool> Handle(MarkReminderSentCommand request, CancellationToken cancellationToken)
    {
        repository.Create(new Reminder
        { WeekId = request.WeekId, GroupId = request.GroupId, UserId = request.UserId, SentUtc = request.SentUtc });
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}