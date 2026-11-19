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
            if (!await HasAccess(request, cancellationToken)) return Result.Failure(global::VocabularyBot.Infrastructure.Resources.Error.AccessDenied);
            return await Execute(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            try
            {
                OnFailure();
            }
            catch (Exception cleanupException)
            {
                logger.LogError(cleanupException, "Failed to reset state after {Command}", typeof(TCommand).Name);
            }

            logger.LogError(ex, "Command {Command} failed", typeof(TCommand).Name);
            return Result.Failure(
                global::VocabularyBot.Infrastructure.Resources.Error.OperationFailed);
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