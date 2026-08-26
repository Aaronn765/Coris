using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentsDsi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentReferenceFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_EntiteId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResponsableN1Id",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResponsableN2Id",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_RisqueId",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_EntiteId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "EntiteId", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResponsableN1Id_DateDeclaration",
                table: "Incidents",
                columns: new[] { "ResponsableN1Id", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResponsableN2Id_DateDeclaration",
                table: "Incidents",
                columns: new[] { "ResponsableN2Id", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_RisqueId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "RisqueId", "DateDeclaration" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_EntiteId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResponsableN1Id_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResponsableN2Id_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_RisqueId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_EntiteId",
                table: "Incidents",
                column: "EntiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResponsableN1Id",
                table: "Incidents",
                column: "ResponsableN1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResponsableN2Id",
                table: "Incidents",
                column: "ResponsableN2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_RisqueId",
                table: "Incidents",
                column: "RisqueId");
        }
    }
}
