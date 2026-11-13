using VocabularyBot.Domain.Base;
using VocabularyBot.Domain.Words;
namespace VocabularyBot.Domain;
public sealed class WordTranslation : Entity
{
    public Guid VocabularyEntryId { get; set; }
    public LanguageType Type { get; set; }
    public string Meaning { get; set; } = "";
}
