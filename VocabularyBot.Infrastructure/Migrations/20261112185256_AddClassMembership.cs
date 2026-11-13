using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "VocabularyEntries",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyEntries_CourseId",
                table: "VocabularyEntries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMemberships_CourseId_UserId",
                table: "CourseMemberships",
                columns: new[] { "CourseId", "UserId" },
                unique: true);

            // Preserve class enrollment from legacy group memberships, once per student and class.
            migrationBuilder.Sql("""
                INSERT INTO CourseMemberships (Id, CourseId, UserId)
                SELECT (hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))), pairs.CourseId, pairs.UserId
                FROM (SELECT DISTINCT g.CourseId, m.UserId FROM Memberships m JOIN Groups g ON g.Id = m.GroupId) AS pairs;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CourseMemberships_UserId",
                table: "CourseMemberships",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VocabularyEntries_Courses_CourseId",
                table: "VocabularyEntries",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VocabularyEntries_Courses_CourseId",
                table: "VocabularyEntries");

            migrationBuilder.DropTable(
                name: "CourseMemberships");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyEntries_CourseId",
                table: "VocabularyEntries");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "VocabularyEntries");
        }
    }
}
