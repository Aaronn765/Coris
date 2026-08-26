using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentsDsi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentListIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_ApplicationId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CriticiteId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_StatutId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_TypeIncidentId",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ApplicationId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "ApplicationId", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CriticiteId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "CriticiteId", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_DateDeclaration",
                table: "Incidents",
                column: "DateDeclaration");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_DateDeclaration_Id",
                table: "Incidents",
                columns: new[] { "DateDeclaration", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_StatutId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "StatutId", "DateDeclaration" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TypeIncidentId_DateDeclaration",
                table: "Incidents",
                columns: new[] { "TypeIncidentId", "DateDeclaration" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_ApplicationId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CriticiteId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_DateDeclaration_Id",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_StatutId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_TypeIncidentId_DateDeclaration",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ApplicationId",
                table: "Incidents",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CriticiteId",
                table: "Incidents",
                column: "CriticiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_StatutId",
                table: "Incidents",
                column: "StatutId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TypeIncidentId",
                table: "Incidents",
                column: "TypeIncidentId");
        }
    }
}
