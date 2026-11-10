using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;

public sealed class StudyWeekConfiguration : IEntityTypeConfiguration<StudyWeek>
{
    public void Configure(EntityTypeBuilder<StudyWeek> b) { b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict); }
}
