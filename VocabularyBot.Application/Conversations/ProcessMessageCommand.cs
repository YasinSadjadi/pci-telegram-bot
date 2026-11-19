using VocabularyBot.Infrastructure.Abstractions.Messaging.Interfaces;

namespace VocabularyBot.Application.Conversations;

public sealed record ProcessMessageCommand(long UserId, string? Username, string Name, string Text)
    : ICommand<BotReply>;