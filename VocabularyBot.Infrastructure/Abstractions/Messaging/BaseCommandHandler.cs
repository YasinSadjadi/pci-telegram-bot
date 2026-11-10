using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
using VocabularyBot.Infrastructure.Abstractions.Results;

namespace VocabularyBot.Infrastructure.Abstractions.Messaging;

public abstract class BaseCommandHandler<TCommand>(ILoggerFactory loggerFactory) : ICommandHandler<TCommand, Result>
    where TCommand : ICommand<Result>
{
    private readonly ILogger logger = loggerFactory.CreateLogger(typeof(TCommand));

    public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await HasAccess(request, cancellationToken)) return Result.Failure("دسترسی ندارید.");
            return await Execute(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException
                                       or DbUpdateException)
        {
            OnFailure();
            logger.LogWarning("Command {Command} failed with {Error}", typeof(TCommand).Name, ex.GetType().Name);
            return Result.Failure("عملیات انجام نشد: " +
                                  (ex is DbUpdateException ? "اطلاعات تکراری یا نامعتبر است." : ex.Message) +
                                  "\nلطفاً دوباره شروع کنید.");
        }
    }

    protected virtual Task<bool> HasAccess(TCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    protected virtual void OnFailure()
    {
    }

    protected abstract Task<Result> Execute(TCommand request, CancellationToken cancellationToken);
}