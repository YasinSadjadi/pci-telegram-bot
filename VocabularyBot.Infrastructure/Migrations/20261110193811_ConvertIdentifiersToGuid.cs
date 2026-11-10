using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace VocabularyBot.Infrastructure.Migrations;

public partial class ConvertIdentifiersToGuid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Stable type-specific mappings preserve all legacy relationships and Telegram identifiers.
        migrationBuilder.Sql("""

CREATE TABLE "__new_Users" ("Id" TEXT NOT NULL PRIMARY KEY, "TelegramId" INTEGER NOT NULL, "Username" TEXT NULL, "Name" TEXT NOT NULL, "IsAdmin" INTEGER NOT NULL);
CREATE TABLE "__new_Courses" ("Id" TEXT NOT NULL PRIMARY KEY, "University" TEXT NOT NULL, "Year" INTEGER NOT NULL, "Term" INTEGER NOT NULL);
CREATE TABLE "__new_Groups" ("Id" TEXT NOT NULL PRIMARY KEY, "CourseId" TEXT NOT NULL, "Number" INTEGER NOT NULL, "LeaderId" TEXT NULL, FOREIGN KEY ("CourseId") REFERENCES "__new_Courses"("Id") ON DELETE RESTRICT);
CREATE TABLE "__new_Weeks" ("Id" TEXT NOT NULL PRIMARY KEY, "CourseId" TEXT NOT NULL, "Label" TEXT NOT NULL, "StartUtc" TEXT NOT NULL, "EndUtc" TEXT NOT NULL, "ReminderUtc" TEXT NOT NULL, FOREIGN KEY ("CourseId") REFERENCES "__new_Courses"("Id") ON DELETE RESTRICT);
CREATE TABLE "__new_Memberships" ("Id" TEXT NOT NULL PRIMARY KEY, "GroupId" TEXT NOT NULL, "UserId" TEXT NOT NULL, FOREIGN KEY ("GroupId") REFERENCES "__new_Groups"("Id") ON DELETE RESTRICT, FOREIGN KEY ("UserId") REFERENCES "__new_Users"("Id") ON DELETE RESTRICT);
CREATE TABLE "__new_Conversations" ("Id" TEXT NOT NULL PRIMARY KEY, "UserId" TEXT NOT NULL, "Flow" TEXT NOT NULL, "Step" INTEGER NOT NULL, "Data" TEXT NOT NULL);
CREATE TABLE "__new_Reminders" ("Id" TEXT NOT NULL PRIMARY KEY, "WeekId" TEXT NOT NULL, "GroupId" TEXT NOT NULL, "UserId" TEXT NOT NULL, "SentUtc" TEXT NOT NULL);
CREATE TABLE "__new_Words" ("Id" TEXT NOT NULL PRIMARY KEY, "CourseId" TEXT NOT NULL, "GroupId" TEXT NOT NULL, "WeekId" TEXT NOT NULL, "UserId" TEXT NOT NULL, "Title" TEXT NOT NULL, "NormalizedTitle" TEXT NOT NULL, "Meaning" TEXT NOT NULL, "Definition" TEXT NOT NULL, "Example" TEXT NOT NULL, "Related" TEXT NOT NULL, "Source" TEXT NOT NULL, FOREIGN KEY ("CourseId") REFERENCES "__new_Courses"("Id") ON DELETE RESTRICT, FOREIGN KEY ("GroupId") REFERENCES "__new_Groups"("Id") ON DELETE RESTRICT, FOREIGN KEY ("WeekId") REFERENCES "__new_Weeks"("Id") ON DELETE RESTRICT, FOREIGN KEY ("UserId") REFERENCES "__new_Users"("Id") ON DELETE RESTRICT);
INSERT INTO "__new_Users" SELECT upper(printf('00000001-0000-0000-%04x-%012x', (Id >> 48) & 65535, Id & 281474976710655)), Id, Username, Name, IsAdmin FROM Users;
INSERT INTO "__new_Courses" SELECT upper(printf('00000002-0000-0000-%04x-%012x', (Id >> 48) & 65535, Id & 281474976710655)), University, Year, Term FROM Courses;
INSERT INTO "__new_Groups" SELECT upper(printf('00000003-0000-0000-%04x-%012x', (Id >> 48) & 65535, Id & 281474976710655)), upper(printf('00000002-0000-0000-%04x-%012x', (CourseId >> 48) & 65535, CourseId & 281474976710655)), Number, CASE WHEN LeaderId IS NULL THEN NULL ELSE upper(printf('00000001-0000-0000-%04x-%012x', (LeaderId >> 48) & 65535, LeaderId & 281474976710655)) END FROM Groups;
INSERT INTO "__new_Weeks" SELECT upper(printf('00000004-0000-0000-%04x-%012x', (Id >> 48) & 65535, Id & 281474976710655)), upper(printf('00000002-0000-0000-%04x-%012x', (CourseId >> 48) & 65535, CourseId & 281474976710655)), Label, StartUtc, EndUtc, ReminderUtc FROM Weeks;
INSERT INTO "__new_Memberships" SELECT (substr(hex(randomblob(4)),1,8)||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(6))), upper(printf('00000003-0000-0000-%04x-%012x', (GroupId >> 48) & 65535, GroupId & 281474976710655)), upper(printf('00000001-0000-0000-%04x-%012x', (UserId >> 48) & 65535, UserId & 281474976710655)) FROM Memberships;
INSERT INTO "__new_Conversations" SELECT (substr(hex(randomblob(4)),1,8)||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(6))), upper(printf('00000001-0000-0000-%04x-%012x', (UserId >> 48) & 65535, UserId & 281474976710655)), Flow, Step, Data FROM Conversations;
INSERT INTO "__new_Reminders" SELECT (substr(hex(randomblob(4)),1,8)||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(2))||'-'||hex(randomblob(6))), upper(printf('00000004-0000-0000-%04x-%012x', (WeekId >> 48) & 65535, WeekId & 281474976710655)), upper(printf('00000003-0000-0000-%04x-%012x', (GroupId >> 48) & 65535, GroupId & 281474976710655)), upper(printf('00000001-0000-0000-%04x-%012x', (UserId >> 48) & 65535, UserId & 281474976710655)), SentUtc FROM Reminders;
INSERT INTO "__new_Words" SELECT upper(printf('00000005-0000-0000-%04x-%012x', (Id >> 48) & 65535, Id & 281474976710655)), upper(printf('00000002-0000-0000-%04x-%012x', (CourseId >> 48) & 65535, CourseId & 281474976710655)), upper(printf('00000003-0000-0000-%04x-%012x', (GroupId >> 48) & 65535, GroupId & 281474976710655)), upper(printf('00000004-0000-0000-%04x-%012x', (WeekId >> 48) & 65535, WeekId & 281474976710655)), upper(printf('00000001-0000-0000-%04x-%012x', (UserId >> 48) & 65535, UserId & 281474976710655)), Title, NormalizedTitle, Meaning, Definition, Example, Related, Source FROM Words;
DROP TABLE Words; DROP TABLE Memberships; DROP TABLE Reminders; DROP TABLE Conversations; DROP TABLE Groups; DROP TABLE Weeks; DROP TABLE Courses; DROP TABLE Users;
ALTER TABLE "__new_Users" RENAME TO Users; ALTER TABLE "__new_Courses" RENAME TO Courses; ALTER TABLE "__new_Groups" RENAME TO Groups; ALTER TABLE "__new_Weeks" RENAME TO Weeks; ALTER TABLE "__new_Memberships" RENAME TO Memberships; ALTER TABLE "__new_Conversations" RENAME TO Conversations; ALTER TABLE "__new_Reminders" RENAME TO Reminders; ALTER TABLE "__new_Words" RENAME TO Words;
CREATE UNIQUE INDEX "IX_Users_TelegramId" ON Users(TelegramId);
CREATE UNIQUE INDEX "IX_Groups_CourseId_Number" ON Groups(CourseId, Number);
CREATE UNIQUE INDEX "IX_Memberships_GroupId_UserId" ON Memberships(GroupId, UserId);
CREATE INDEX "IX_Memberships_UserId" ON Memberships(UserId);
CREATE UNIQUE INDEX "IX_Conversations_UserId" ON Conversations(UserId);
CREATE UNIQUE INDEX "IX_Reminders_WeekId_GroupId_UserId" ON Reminders(WeekId, GroupId, UserId);
CREATE INDEX "IX_Weeks_CourseId" ON Weeks(CourseId);
CREATE UNIQUE INDEX "IX_Words_CourseId_NormalizedTitle" ON Words(CourseId, NormalizedTitle);
CREATE INDEX "IX_Words_GroupId" ON Words(GroupId); CREATE INDEX "IX_Words_WeekId" ON Words(WeekId); CREATE INDEX "IX_Words_UserId" ON Words(UserId);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("GUID identifiers cannot be safely converted back to the legacy numeric schema. Restore a backup instead.");
    }
}
