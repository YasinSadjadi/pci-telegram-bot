using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;
namespace VocabularyBot.Application.Words.ListMyWords;
public sealed record ListMyWordsQuery(long TelegramId) : IQuery<string>;
