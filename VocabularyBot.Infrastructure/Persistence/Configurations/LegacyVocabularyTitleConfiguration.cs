using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;
public sealed class LegacyVocabularyTitleConfiguration : IEntityTypeConfiguration<LegacyVocabularyTitle>
{
    public void Configure(EntityTypeBuilder<LegacyVocabularyTitle> builder) => builder.HasIndex(x => x.NormalizedTitle).IsUnique();
}
