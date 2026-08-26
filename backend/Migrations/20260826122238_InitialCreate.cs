using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IncidentsDsi.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
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
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Criticites",
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
                    table.PrimaryKey("PK_Criticites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entites",
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
                    table.PrimaryKey("PK_Entites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentNumberSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNumberSequences", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "Responsables",
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
                    table.PrimaryKey("PK_Responsables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Risques",
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
                    table.PrimaryKey("PK_Risques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statuts",
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
                    table.PrimaryKey("PK_Statuts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesIncident",
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
                    table.PrimaryKey("PK_TypesIncident", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateDeclaration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TypeIncidentId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Intitule = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    EntiteId = table.Column<int>(type: "int", nullable: false),
                    CriticiteId = table.Column<int>(type: "int", nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Cause = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    RisqueId = table.Column<int>(type: "int", nullable: false),
                    ActionsMenees = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Solution = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    ActionsEnCours = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    ResponsableN1Id = table.Column<int>(type: "int", nullable: false),
                    ResponsableN2Id = table.Column<int>(type: "int", nullable: true),
                    MesuresPreventives = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    StatutId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Criticites_CriticiteId",
                        column: x => x.CriticiteId,
                        principalTable: "Criticites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Entites_EntiteId",
                        column: x => x.EntiteId,
                        principalTable: "Entites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Responsables_ResponsableN1Id",
                        column: x => x.ResponsableN1Id,
                        principalTable: "Responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Responsables_ResponsableN2Id",
                        column: x => x.ResponsableN2Id,
                        principalTable: "Responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Risques_RisqueId",
                        column: x => x.RisqueId,
                        principalTable: "Risques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Statuts_StatutId",
                        column: x => x.StatutId,
                        principalTable: "Statuts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_TypesIncident_TypeIncidentId",
                        column: x => x.TypeIncidentId,
                        principalTable: "TypesIncident",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ABM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AMPLITUDE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CARTHAGO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SMARTACCESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SWIFT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TOUTES LES APPLICATIONS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VNEURON", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Criticites",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fort", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moyenne", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Entites",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2-PLATEAUX", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ABATTA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ADJAME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AGENCE ABATTA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AGENCE ANGRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AGENCE DALOA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AGENCE KOUMASSI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ANGRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BANQUE PRIVEE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BARDO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BOUAKE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CBICI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DALOA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DUEKOUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "KOUMASSI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PLATEAU", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SAN PEDRO BARDOT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SAN PEDRO CITE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TREICHVILLE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "YOP 3E PONT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Responsables",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ARSENE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ARSERNE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DOUASSE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "EMMANUEL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Equipe Exploitation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "HOLDING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "KARAMOKO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "LEGRE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MATHURIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "NICAISE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PRESTATAIRE ELECTRIQUE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PRESTATAIRE SMART ACCESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SOMTORE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "BCEAO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Risques",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fort", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moyen", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Statuts",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "En cours", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clôturé", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "TypesIncident",
                columns: new[] { "Id", "Actif", "CreatedAt", "Nom", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Infrastructure / électrique", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "application", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "réseaux", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Nom",
                table: "Applications",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Criticites_Nom",
                table: "Criticites",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Entites_Nom",
                table: "Entites",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ApplicationId",
                table: "Incidents",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CriticiteId",
                table: "Incidents",
                column: "CriticiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_EntiteId",
                table: "Incidents",
                column: "EntiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Numero",
                table: "Incidents",
                column: "Numero",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_StatutId",
                table: "Incidents",
                column: "StatutId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TypeIncidentId",
                table: "Incidents",
                column: "TypeIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsables_Nom",
                table: "Responsables",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Risques_Nom",
                table: "Risques",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Statuts_Nom",
                table: "Statuts",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_TypesIncident_Nom",
                table: "TypesIncident",
                column: "Nom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentNumberSequences");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Criticites");

            migrationBuilder.DropTable(
                name: "Entites");

            migrationBuilder.DropTable(
                name: "Responsables");

            migrationBuilder.DropTable(
                name: "Risques");

            migrationBuilder.DropTable(
                name: "Statuts");

            migrationBuilder.DropTable(
                name: "TypesIncident");
        }
    }
}
