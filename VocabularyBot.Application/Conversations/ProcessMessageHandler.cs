using Microsoft.Extensions.Logging;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Conversations;

public sealed class ProcessMessageHandler(BotService service, ILogger<ProcessMessageHandler> logger)
    : ICommandHandler<ProcessMessageCommand, BotReply>
{
    public async Task<BotReply> Handle(ProcessMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var text = await service.Handle(request.UserId, request.Username, request.Name, request.Text);
            return new BotReply(text, await service.Keyboard(request.UserId, request.Text));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process a Telegram message");
            return new BotReply("متأسفانه در پردازش پیام مشکلی پیش آمد. لطفاً دوباره تلاش کنید؛ اگر مشکل ادامه داشت با پشتیبانی تماس بگیرید.");
        }
    }
}
