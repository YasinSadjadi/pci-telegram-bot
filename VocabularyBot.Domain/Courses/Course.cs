using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class Course : Entity
{
    public string University { get; set; } = "";

    public int Year { get; set; }

    public int Term { get; set; }
}