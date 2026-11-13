using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;
public sealed class CourseMembershipConfiguration : IEntityTypeConfiguration<CourseMembership>
{
    public void Configure(EntityTypeBuilder<CourseMembership> builder)
    {
        builder.HasIndex(x => new { x.CourseId, x.UserId }).IsUnique();
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
