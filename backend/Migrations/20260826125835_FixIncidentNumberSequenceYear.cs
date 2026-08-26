using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentsDsi.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixIncidentNumberSequenceYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE [dbo].[IncidentNumberSequences_New]
                (
                    [Year] int NOT NULL,
                    [LastValue] int NOT NULL,
                    CONSTRAINT [PK_IncidentNumberSequences_New] PRIMARY KEY ([Year])
                );

                INSERT INTO [dbo].[IncidentNumberSequences_New] ([Year], [LastValue])
                SELECT [Year], [LastValue]
                FROM [dbo].[IncidentNumberSequences];

                DROP TABLE [dbo].[IncidentNumberSequences];
                EXEC sp_rename N'dbo.IncidentNumberSequences_New', N'IncidentNumberSequences';
                EXEC sp_rename N'dbo.PK_IncidentNumberSequences_New', N'PK_IncidentNumberSequences', N'OBJECT';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE [dbo].[IncidentNumberSequences_Old]
                (
                    [Year] int IDENTITY(1, 1) NOT NULL,
                    [LastValue] int NOT NULL,
                    CONSTRAINT [PK_IncidentNumberSequences_Old] PRIMARY KEY ([Year])
                );

                SET IDENTITY_INSERT [dbo].[IncidentNumberSequences_Old] ON;
                INSERT INTO [dbo].[IncidentNumberSequences_Old] ([Year], [LastValue])
                SELECT [Year], [LastValue]
                FROM [dbo].[IncidentNumberSequences];
                SET IDENTITY_INSERT [dbo].[IncidentNumberSequences_Old] OFF;

                DROP TABLE [dbo].[IncidentNumberSequences];
                EXEC sp_rename N'dbo.IncidentNumberSequences_Old', N'IncidentNumberSequences';
                EXEC sp_rename N'dbo.PK_IncidentNumberSequences_Old', N'PK_IncidentNumberSequences', N'OBJECT';
                """);
        }
    }
}
