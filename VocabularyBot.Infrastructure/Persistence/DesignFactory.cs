using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VocabularyBot.Infrastructure;

public class DesignFactory : IDesignTimeDbContextFactory<BotDb>
{
    public BotDb CreateDbContext(string[] args)
    {
        return new BotDb(new DbContextOptionsBuilder<BotDb>().UseSqlite("Data Source=design.db").Options);
    }
}