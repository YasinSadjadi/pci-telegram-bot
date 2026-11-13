using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;
public sealed class VocabularyEntryConfiguration : IEntityTypeConfiguration<VocabularyEntry>
{
 public void Configure(EntityTypeBuilder<VocabularyEntry> b)
 {
  b.HasIndex(x => x.NormalizedWord).IsUnique();
  b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
 }
}
