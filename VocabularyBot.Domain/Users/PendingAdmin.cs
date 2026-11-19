using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public sealed class PendingAdmin : Entity
{
    public string Username { get; init; } = "";
}
