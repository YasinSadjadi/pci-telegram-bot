using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;

public class Membership : Entity
{
    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }
}