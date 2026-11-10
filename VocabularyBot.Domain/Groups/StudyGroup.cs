using VocabularyBot.Domain.Base;

namespace VocabularyBot.Domain;

public class StudyGroup : Entity
{
    public Guid CourseId { get; set; }

    public int Number { get; set; }

    public Guid? LeaderId { get; set; }
}