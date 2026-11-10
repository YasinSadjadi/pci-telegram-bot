using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class StudyWeek : Entity
{
    public Guid CourseId { get; set; }

    public string Label { get; set; } = "";
    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public DateTime ReminderUtc { get; set; }
}