using System.ComponentModel.DataAnnotations;
namespace VocabularyBot.Domain.Words;

public enum LanguageType
{
    [Display(Name = "آلمانی")]
    German = 1,
    [Display(Name = "فرانسوی")]
    French = 2,
    [Display(Name = "اسپانیایی")]
    Spanish = 3
}
