using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Conversations;

public sealed class ProcessMessageHandler(BotService service) : ICommandHandler<ProcessMessageCommand, string>
{
    public Task<string> Handle(ProcessMessageCommand request, CancellationToken cancellationToken)
    {
        return service.Handle(request.UserId, request.Username, request.Name, request.Text);
    }
}