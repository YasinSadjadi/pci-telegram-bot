using MediatR;

namespace VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

public interface ICommand<out T> : IRequest<T>
{
}