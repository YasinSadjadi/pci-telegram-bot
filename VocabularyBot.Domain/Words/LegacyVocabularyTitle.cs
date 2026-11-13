using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;
public sealed class LegacyVocabularyTitle : Entity
{
    public string NormalizedTitle { get; set; } = "";
}
