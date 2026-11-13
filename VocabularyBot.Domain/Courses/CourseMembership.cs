using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;
public sealed class CourseMembership : Entity
{
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
}
