using Microsoft.EntityFrameworkCore;
using VocabularyBot.Domain;

namespace VocabularyBot.Infrastructure;

public class BotDb(DbContextOptions<BotDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseMembership> CourseMemberships => Set<CourseMembership>();
    public DbSet<StudyGroup> Groups => Set<StudyGroup>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<StudyWeek> Weeks => Set<StudyWeek>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<VocabularyEntry> VocabularyEntries => Set<VocabularyEntry>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Conversation> Conversations => Set<Conversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotDb).Assembly);
    }
}
