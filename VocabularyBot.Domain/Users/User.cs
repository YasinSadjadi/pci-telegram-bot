using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class User : Entity
{
    public long TelegramId { get; set; }

    public string? Username { get; set; }

    public string Name { get; set; } = "";

    public bool IsAdmin { get; set; }
}