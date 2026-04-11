using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IcuNotes.Migrations
{
    /// <inheritdoc />
    public partial class ReCreatingEverything : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionUnitCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionUnitCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedPatients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: true),
                    Diagnosis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CombinedHistory = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedPatients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: true),
                    Bed = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    Diagnosis = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AdmissionUnitCatalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_AdmissionUnitCatalogs_AdmissionUnitCatalogId",
                        column: x => x.AdmissionUnitCatalogId,
                        principalTable: "AdmissionUnitCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedPatientDateEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArchivedPatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedPatientDateEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivedPatientDateEvents_ArchivedPatients_ArchivedPatientId",
                        column: x => x.ArchivedPatientId,
                        principalTable: "ArchivedPatients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Neurologies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    GcsEye = table.Column<int>(type: "INTEGER", nullable: true),
                    GcsVerbal = table.Column<int>(type: "INTEGER", nullable: true),
                    GcsMotor = table.Column<int>(type: "INTEGER", nullable: true),
                    Pupils = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MotorStatus = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neurologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Neurologies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientDateEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDateEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDateEvents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CombinedHistory = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    EventsTodo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientSummaries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NeurologyMedications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NeurologyId = table.Column<int>(type: "INTEGER", nullable: false),
                    MedicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Dose = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeurologyMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NeurologyMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NeurologyMedications_Neurologies_NeurologyId",
                        column: x => x.NeurologyId,
                        principalTable: "Neurologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedPatientDateEvents_ArchivedPatientId",
                table: "ArchivedPatientDateEvents",
                column: "ArchivedPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Neurologies_PatientId",
                table: "Neurologies",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NeurologyMedications_MedicationId",
                table: "NeurologyMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_NeurologyMedications_NeurologyId",
                table: "NeurologyMedications",
                column: "NeurologyId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDateEvents_PatientId",
                table: "PatientDateEvents",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AdmissionUnitCatalogId",
                table: "Patients",
                column: "AdmissionUnitCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientSummaries_PatientId",
                table: "PatientSummaries",
                column: "PatientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivedPatientDateEvents");

            migrationBuilder.DropTable(
                name: "NeurologyMedications");

            migrationBuilder.DropTable(
                name: "PatientDateEvents");

            migrationBuilder.DropTable(
                name: "PatientSummaries");

            migrationBuilder.DropTable(
                name: "ArchivedPatients");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Neurologies");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AdmissionUnitCatalogs");
        }
    }
}
