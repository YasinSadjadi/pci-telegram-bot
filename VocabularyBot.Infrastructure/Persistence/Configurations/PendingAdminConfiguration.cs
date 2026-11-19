using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;

namespace VocabularyBot.Infrastructure.Persistence.Configurations;

public sealed class PendingAdminConfiguration : IEntityTypeConfiguration<PendingAdmin>
{
    public void Configure(EntityTypeBuilder<PendingAdmin> b)
    {
        b.ToTable("PendingAdmins");
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Username).HasMaxLength(32);
    }
}
