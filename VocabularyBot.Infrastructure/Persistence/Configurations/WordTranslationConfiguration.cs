using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;
public sealed class WordTranslationConfiguration : IEntityTypeConfiguration<WordTranslation>
{
    public void Configure(EntityTypeBuilder<WordTranslation> builder)
    {
        builder.HasIndex(x => new { x.VocabularyEntryId, x.Type }).IsUnique();
        builder.HasOne<VocabularyEntry>().WithMany().HasForeignKey(x => x.VocabularyEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}
