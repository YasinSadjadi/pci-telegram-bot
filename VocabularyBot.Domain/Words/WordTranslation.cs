using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;
public sealed class WordTranslation : Entity
{
    public Guid VocabularyEntryId { get; set; }
    public string Language { get; set; } = "";
    public string NormalizedLanguage { get; set; } = "";
    public string Meaning { get; set; } = "";
}
