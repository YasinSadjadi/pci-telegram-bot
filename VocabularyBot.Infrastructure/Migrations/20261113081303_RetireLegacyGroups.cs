using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace VocabularyBot.Infrastructure.Migrations;

public partial class RetireLegacyGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Old tables are intentionally left untouched so existing records are not deleted.
        migrationBuilder.CreateTable(
            name: "LegacyVocabularyTitles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                NormalizedTitle = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LegacyVocabularyTitles", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_LegacyVocabularyTitles_NormalizedTitle", table: "LegacyVocabularyTitles", column: "NormalizedTitle", unique: true);
        migrationBuilder.Sql("""
            INSERT INTO LegacyVocabularyTitles (Id, NormalizedTitle)
            SELECT hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6)), words.NormalizedTitle
            FROM (SELECT DISTINCT NormalizedTitle FROM Words) AS words;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LegacyVocabularyTitles");
    }
}
