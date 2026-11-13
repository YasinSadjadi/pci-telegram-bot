using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VocabularyBot.Infrastructure;

namespace VocabularyBot.Tests;

public sealed class WordTranslationMigrationTests
{
    [Fact]
    public async Task Migration_preserves_existing_optional_family_and_free_text_translations()
    {
        var path = Path.GetTempFileName();
        try
        {
            await using var db = new BotDb(new DbContextOptionsBuilder<BotDb>().UseSqlite($"Data Source={path};Pooling=False").Options);
            await db.GetService<IMigrator>().MigrateAsync("20261113081303_RetireLegacyGroups");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Users (Id, TelegramId, StudentNumber, Username, Name, IsAdmin)
                VALUES ('00000001-0000-0000-0000-000000000001', 42, '123', NULL, 'Student', 0);
                INSERT INTO VocabularyEntries (Id, UserId, CourseId, EnglishWord, NormalizedWord, Pronunciation, PersianMeaning, Family, Synonyms, Translations)
                VALUES ('00000002-0000-0000-0000-000000000001', '00000001-0000-0000-0000-000000000001', NULL, 'example', 'EXAMPLE', 'pronunciation', 'معنی', NULL, NULL, 'Deutsch: Beispiel');
                """);
            await db.Database.MigrateAsync();
            var word = await db.VocabularyEntries.SingleAsync();
            Assert.Null(word.Family);
            Assert.Equal("Deutsch: Beispiel", word.Translations);
            Assert.Null(word.GermanTranslation);
            Assert.Null(word.FrenchTranslation);
            Assert.Null(word.SpanishTranslation);
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public async Task Previously_saved_language_columns_are_copied_to_flexible_translations()
    {
        var path = Path.GetTempFileName();
        try
        {
            await using var db = new BotDb(new DbContextOptionsBuilder<BotDb>().UseSqlite($"Data Source={path};Pooling=False").Options);
            await db.GetService<IMigrator>().MigrateAsync("20261113081620_AddWordTranslations");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Users (Id, TelegramId, StudentNumber, Username, Name, IsAdmin)
                VALUES ('00000001-0000-0000-0000-000000000001', 42, '123', NULL, 'Student', 0);
                INSERT INTO VocabularyEntries (Id, UserId, CourseId, EnglishWord, NormalizedWord, Pronunciation, PersianMeaning, Family, Synonyms, Translations, GermanTranslation, FrenchTranslation, SpanishTranslation)
                VALUES ('00000002-0000-0000-0000-000000000001', '00000001-0000-0000-0000-000000000001', NULL, 'example', 'EXAMPLE', 'pronunciation', 'معنی', 'family', NULL, 'ایتالیایی: esempio', 'Beispiel', 'exemple', 'ejemplo');
                """);
            await db.Database.MigrateAsync();
            var translations = await db.WordTranslations.ToListAsync();
            Assert.Equal(3, translations.Count);
            Assert.Contains(translations, t => t.Language == "آلمانی" && t.Meaning == "Beispiel");
            Assert.Contains(translations, t => t.Language == "فرانسوی" && t.Meaning == "exemple");
            Assert.Contains(translations, t => t.Language == "اسپانیایی" && t.Meaning == "ejemplo");
            Assert.Equal("ایتالیایی: esempio", (await db.VocabularyEntries.SingleAsync()).Translations);
        }
        finally { File.Delete(path); }
    }

}
