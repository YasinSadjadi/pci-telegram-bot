using VocabularyBot.Domain.Base;
namespace VocabularyBot.Domain;

public class Reminder : Entity
{
    public Guid WeekId { get; set; }

    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public DateTime SentUtc { get; set; }
}