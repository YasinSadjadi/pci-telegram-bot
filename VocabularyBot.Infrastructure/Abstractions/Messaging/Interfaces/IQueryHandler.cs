using MediatR;

namespace VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
}