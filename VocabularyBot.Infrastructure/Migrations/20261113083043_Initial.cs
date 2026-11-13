using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Flow = table.Column<string>(type: "TEXT", nullable: false),
                    Step = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    University = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Term = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentNumber = table.Column<string>(type: "TEXT", nullable: true),
                    TelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMemberships_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnglishWord = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedWord = table.Column<string>(type: "TEXT", nullable: false),
                    Pronunciation = table.Column<string>(type: "TEXT", nullable: false),
                    PersianMeaning = table.Column<string>(type: "TEXT", nullable: false),
                    Family = table.Column<string>(type: "TEXT", nullable: false),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyEntries_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VocabularyEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WordTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VocabularyEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseMemberships_CourseId_UserId",
                table: "CourseMemberships",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseMemberships_UserId",
                table: "CourseMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentNumber",
                table: "Users",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_CourseId",
                table: "VocabularyEntries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_NormalizedWord",
                table: "VocabularyEntries",
                column: "NormalizedWord",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_UserId",
                table: "VocabularyEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordTranslations_VocabularyEntryId_Type",
                table: "WordTranslations",
                columns: new[] { "VocabularyEntryId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "CourseMemberships");

            migrationBuilder.DropTable(
                name: "WordTranslations");

            migrationBuilder.DropTable(
                name: "VocabularyEntries");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
