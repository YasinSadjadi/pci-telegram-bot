using MediatR;

namespace VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
}