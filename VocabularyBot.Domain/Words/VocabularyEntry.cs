using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;
public sealed class VocabularyEntry : Entity
{
 public Guid? CourseId { get; set; }
 public Guid UserId { get; set; }
 public string EnglishWord { get; set; } = "";
 public string NormalizedWord { get; set; } = "";
 public string Pronunciation { get; set; } = "";
 public string PersianMeaning { get; set; } = "";
 public string? Family { get; set; }
 public string? Synonyms { get; set; }
 public string? Translations { get; set; }
}
