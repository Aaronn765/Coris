using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IncidentsDsi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainesProjet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainesProjet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjetNumberSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetNumberSequences", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "StatutsEtape",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutsEtape", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatutsProjet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutsProjet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DomaineProjetId = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    TauxAvancement = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateEcheance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatutProjetId = table.Column<int>(type: "int", nullable: false),
                    Support = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    ActeursMetiers = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Contraintes = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Commentaires = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projets_DomainesProjet_DomaineProjetId",
                        column: x => x.DomaineProjetId,
                        principalTable: "DomainesProjet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projets_StatutsProjet_StatutProjetId",
                        column: x => x.StatutProjetId,
                        principalTable: "StatutsProjet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EtapesProjet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjetId = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    TauxAvancement = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    StatutEtapeId = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Support = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Contraintes = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Commentaires = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesProjet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesProjet_Projets_ProjetId",
                        column: x => x.ProjetId,
                        principalTable: "Projets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EtapesProjet_StatutsEtape_StatutEtapeId",
                        column: x => x.StatutEtapeId,
                        principalTable: "StatutsEtape",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjetResponsables",
                columns: table => new
                {
                    ProjetId = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetResponsables", x => new { x.ProjetId, x.ResponsableId });
                    table.ForeignKey(
                        name: "FK_ProjetResponsables_Projets_ProjetId",
                        column: x => x.ProjetId,
                        principalTable: "Projets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjetResponsables_Responsables_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DomainesProjet",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MONETIQUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SECURITE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "REGLEMENTAIRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "INFRA RESEAU & SYSTÈME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "APPLICATIONS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CORE BANKING AMPLITUDE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FILE D'ATTENTE CLIENTELE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PRODUCTION INFORMATIQUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "HELPDESK & PARC INFORMATIQUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ETATS & REPORTING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "REPORTING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "StatutsEtape",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Non démarrée", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Démarrée", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Terminée", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "StatutsProjet",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Planifié", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "En cours", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clôturé", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Suspendu", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainesProjet_Nom",
                table: "DomainesProjet",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesProjet_ProjetId_Ordre",
                table: "EtapesProjet",
                columns: new[] { "ProjetId", "Ordre" });

            migrationBuilder.CreateIndex(
                name: "IX_EtapesProjet_StatutEtapeId",
                table: "EtapesProjet",
                column: "StatutEtapeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetResponsables_ResponsableId",
                table: "ProjetResponsables",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_DateEcheance",
                table: "Projets",
                column: "DateEcheance");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_DomaineProjetId_DateEcheance",
                table: "Projets",
                columns: new[] { "DomaineProjetId", "DateEcheance" });

            migrationBuilder.CreateIndex(
                name: "IX_Projets_Numero",
                table: "Projets",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projets_StatutProjetId_DateEcheance",
                table: "Projets",
                columns: new[] { "StatutProjetId", "DateEcheance" });

            migrationBuilder.CreateIndex(
                name: "IX_StatutsEtape_Nom",
                table: "StatutsEtape",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_StatutsProjet_Nom",
                table: "StatutsProjet",
                column: "Nom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtapesProjet");

            migrationBuilder.DropTable(
                name: "ProjetNumberSequences");

            migrationBuilder.DropTable(
                name: "ProjetResponsables");

            migrationBuilder.DropTable(
                name: "StatutsEtape");

            migrationBuilder.DropTable(
                name: "Projets");

            migrationBuilder.DropTable(
                name: "DomainesProjet");

            migrationBuilder.DropTable(
                name: "StatutsProjet");
        }
    }
}
