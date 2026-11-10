using MediatR;

namespace VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

public interface IQuery<out T> : IRequest<T>
{
}