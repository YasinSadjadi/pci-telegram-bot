using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocabularyBot.Domain;
namespace VocabularyBot.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b) { b.HasIndex(x => x.TelegramId).IsUnique(); b.HasIndex(x => x.StudentNumber).IsUnique(); }
}
