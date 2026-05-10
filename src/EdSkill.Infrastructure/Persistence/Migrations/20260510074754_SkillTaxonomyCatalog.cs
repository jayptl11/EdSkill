using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdSkill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkillTaxonomyCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Aliases = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "N'[]'"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.SkillId);
                });

            migrationBuilder.CreateTable(
                name: "UserSkills",
                columns: table => new
                {
                    UserSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkills", x => x.UserSkillId);
                    table.ForeignKey(
                        name: "FK_UserSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSkills_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Slug",
                table: "Skills",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_SkillId",
                table: "UserSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserId_SkillId_Type",
                table: "UserSkills",
                columns: new[] { "UserId", "SkillId", "Type" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Skills (SkillId, Name, Slug, Category, Aliases, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0001', N'Speaking', N'speaking', N'Communication', N'["English","Tiếng Anh","English Speaking"]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0002', N'CV', N'cv', N'Career', N'["Resume"]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0003', N'Interview', N'interview', N'Career', N'["Phỏng vấn"]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0004', N'Excel', N'excel', N'Productivity', N'[]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0005', N'PowerPoint', N'powerpoint', N'Productivity', N'["Power Point"]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0006', N'Canva', N'canva', N'Design', N'[]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0007', N'AI Tools', N'ai-tools', N'AI', N'["AI Tool"]', 1, GETUTCDATE(), GETUTCDATE()),
                    ('8E5A3F2C-8D35-4D83-8E85-A3156A5E0008', N'Presentation', N'presentation', N'Communication', N'["Thuyết trình"]', 1, GETUTCDATE(), GETUTCDATE());

                CREATE TABLE #LegacyProfileSkills
                (
                    UserId uniqueidentifier NOT NULL,
                    RawSkill nvarchar(200) NOT NULL,
                    SkillType nvarchar(16) NOT NULL
                );

                INSERT INTO #LegacyProfileSkills (UserId, RawSkill, SkillType)
                SELECT up.UserId, LTRIM(RTRIM(skills.[value])), N'Teach'
                FROM UserProfiles up
                CROSS APPLY OPENJSON(COALESCE(NULLIF(up.SkillsToTeach, N''), N'[]')) skills
                WHERE LTRIM(RTRIM(skills.[value])) <> N'';

                INSERT INTO #LegacyProfileSkills (UserId, RawSkill, SkillType)
                SELECT up.UserId, LTRIM(RTRIM(skills.[value])), N'Learn'
                FROM UserProfiles up
                CROSS APPLY OPENJSON(COALESCE(NULLIF(up.SkillsToLearn, N''), N'[]')) skills
                WHERE LTRIM(RTRIM(skills.[value])) <> N'';

                CREATE TABLE #LegacyDistinctSkills
                (
                    RawSkill nvarchar(200) NOT NULL PRIMARY KEY,
                    SkillId uniqueidentifier NULL
                );

                INSERT INTO #LegacyDistinctSkills (RawSkill)
                SELECT DISTINCT RawSkill
                FROM #LegacyProfileSkills;

                UPDATE legacy
                SET SkillId = matched.SkillId
                FROM #LegacyDistinctSkills legacy
                OUTER APPLY
                (
                    SELECT TOP (1) s.SkillId
                    FROM Skills s
                    OUTER APPLY OPENJSON(s.Aliases) aliases
                    WHERE
                        s.Name COLLATE Latin1_General_100_CI_AI = legacy.RawSkill COLLATE Latin1_General_100_CI_AI
                        OR s.Slug COLLATE Latin1_General_100_CI_AI = LOWER(
                            REPLACE(
                                REPLACE(
                                    REPLACE(LTRIM(RTRIM(legacy.RawSkill)), N' ', N'-'),
                                    N'--', N'-'
                                ),
                                N'--', N'-'
                            )
                        ) COLLATE Latin1_General_100_CI_AI
                        OR aliases.[value] COLLATE Latin1_General_100_CI_AI = legacy.RawSkill COLLATE Latin1_General_100_CI_AI
                    ORDER BY
                        CASE
                            WHEN s.Name COLLATE Latin1_General_100_CI_AI = legacy.RawSkill COLLATE Latin1_General_100_CI_AI THEN 0
                            WHEN aliases.[value] COLLATE Latin1_General_100_CI_AI = legacy.RawSkill COLLATE Latin1_General_100_CI_AI THEN 1
                            ELSE 2
                        END,
                        s.Name
                ) matched
                WHERE legacy.SkillId IS NULL;

                CREATE TABLE #NewLegacySkills
                (
                    RawSkill nvarchar(200) NOT NULL PRIMARY KEY,
                    SkillId uniqueidentifier NOT NULL
                );

                INSERT INTO #NewLegacySkills (RawSkill, SkillId)
                SELECT RawSkill, NEWID()
                FROM #LegacyDistinctSkills
                WHERE SkillId IS NULL;

                INSERT INTO Skills (SkillId, Name, Slug, Category, Aliases, IsActive, CreatedAt, UpdatedAt)
                SELECT
                    newSkills.SkillId,
                    LEFT(newSkills.RawSkill, 50),
                    N'legacy-' + LOWER(CONVERT(varchar(32), HASHBYTES('MD5', CONVERT(varbinary(max), newSkills.RawSkill)), 2)),
                    NULL,
                    N'[]',
                    0,
                    GETUTCDATE(),
                    GETUTCDATE()
                FROM #NewLegacySkills newSkills;

                UPDATE legacy
                SET SkillId = newSkills.SkillId
                FROM #LegacyDistinctSkills legacy
                INNER JOIN #NewLegacySkills newSkills ON newSkills.RawSkill = legacy.RawSkill
                WHERE legacy.SkillId IS NULL;

                INSERT INTO UserSkills (UserSkillId, UserId, SkillId, Type, CreatedAt)
                SELECT DISTINCT
                    NEWID(),
                    legacy.UserId,
                    mapped.SkillId,
                    legacy.SkillType,
                    GETUTCDATE()
                FROM #LegacyProfileSkills legacy
                INNER JOIN #LegacyDistinctSkills mapped ON mapped.RawSkill = legacy.RawSkill
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM UserSkills existing
                    WHERE existing.UserId = legacy.UserId
                      AND existing.SkillId = mapped.SkillId
                      AND existing.Type = legacy.SkillType
                );

                DROP TABLE #NewLegacySkills;
                DROP TABLE #LegacyDistinctSkills;
                DROP TABLE #LegacyProfileSkills;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSkills");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
