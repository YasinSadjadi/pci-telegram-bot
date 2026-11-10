using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class Conversation : Entity
{
    public Guid UserId { get; set; }

    public string Flow { get; set; } = "";

    public int Step { get; set; }

    public string Data { get; set; } = "[]";
}