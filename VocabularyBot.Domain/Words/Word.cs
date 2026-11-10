using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class Word : Entity
{
    public Guid CourseId { get; set; }

    public Guid GroupId { get; set; }

    public Guid WeekId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = "";

    public string NormalizedTitle { get; set; } = "";

    public string Meaning { get; set; } = "";

    public string Definition { get; set; } = "";

    public string Example { get; set; } = "";

    public string Related { get; set; } = "";

    public string Source { get; set; } = "";
}