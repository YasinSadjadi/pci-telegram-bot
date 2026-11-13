using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WordTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VocabularyEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    Meaning = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordTranslations_VocabularyEntries_VocabularyEntryId",
                        column: x => x.VocabularyEntryId,
                        principalTable: "VocabularyEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO WordTranslations (Id, VocabularyEntryId, Language, NormalizedLanguage, Meaning)
                SELECT hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6)), Id, 'آلمانی', 'آلمانی', TRIM(GermanTranslation) FROM VocabularyEntries WHERE GermanTranslation IS NOT NULL AND TRIM(GermanTranslation) <> ''
                UNION ALL
                SELECT hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6)), Id, 'فرانسوی', 'فرانسوی', TRIM(FrenchTranslation) FROM VocabularyEntries WHERE FrenchTranslation IS NOT NULL AND TRIM(FrenchTranslation) <> ''
                UNION ALL
                SELECT hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6)), Id, 'اسپانیایی', 'اسپانیایی', TRIM(SpanishTranslation) FROM VocabularyEntries WHERE SpanishTranslation IS NOT NULL AND TRIM(SpanishTranslation) <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WordTranslations_VocabularyEntryId_NormalizedLanguage",
                table: "WordTranslations",
                columns: new[] { "VocabularyEntryId", "NormalizedLanguage" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordTranslations");
        }
    }
}
