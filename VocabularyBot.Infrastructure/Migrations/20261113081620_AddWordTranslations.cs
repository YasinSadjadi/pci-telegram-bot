using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWordTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrenchTranslation",
                table: "VocabularyEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GermanTranslation",
                table: "VocabularyEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpanishTranslation",
                table: "VocabularyEntries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrenchTranslation",
                table: "VocabularyEntries");

            migrationBuilder.DropColumn(
                name: "GermanTranslation",
                table: "VocabularyEntries");

            migrationBuilder.DropColumn(
                name: "SpanishTranslation",
                table: "VocabularyEntries");
        }
    }
}
