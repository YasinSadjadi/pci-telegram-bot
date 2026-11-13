using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VocabularyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnglishWord = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedWord = table.Column<string>(type: "TEXT", nullable: false),
                    Pronunciation = table.Column<string>(type: "TEXT", nullable: false),
                    PersianMeaning = table.Column<string>(type: "TEXT", nullable: false),
                    Family = table.Column<string>(type: "TEXT", nullable: true),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: true),
                    Translations = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentNumber",
                table: "Users",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_NormalizedWord",
                table: "VocabularyEntries",
                column: "NormalizedWord",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_UserId",
                table: "VocabularyEntries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyEntries");

            migrationBuilder.DropIndex(
                name: "IX_Users_StudentNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentNumber",
                table: "Users");
        }
    }
}
