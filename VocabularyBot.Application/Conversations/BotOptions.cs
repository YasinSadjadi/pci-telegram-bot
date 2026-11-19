namespace VocabularyBot.Application.Conversations;

public sealed record BotOptions(long? BootstrapAdminId, string? BootstrapAdminUsername = null);