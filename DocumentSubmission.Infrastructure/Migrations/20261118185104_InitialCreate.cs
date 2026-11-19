using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DocumentSubmission.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    EditorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedVersionNumber = table.Column<int>(type: "integer", nullable: true),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Recipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Recipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Users_EditorId",
                        column: x => x.EditorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Recipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Recipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => new { x.SubmissionId, x.Number });
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentContents",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentContents", x => new { x.SubmissionId, x.VersionNumber });
                    table.ForeignKey(
                        name: "FK_DocumentContents_DocumentVersions_SubmissionId_VersionNumber",
                        columns: x => new { x.SubmissionId, x.VersionNumber },
                        principalTable: "DocumentVersions",
                        principalColumns: new[] { "SubmissionId", "Number" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Recipients",
                columns: new[] { "Id", "CountryCode", "Name" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "NL", "Example Records Office" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "NL", "Sample Archive Agency" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "BE", "Fictional Registry Service" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CountryCode", "Name", "Role" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "NL", "Alice Ashford", "Editor" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "NL", "Bram Brink", "Editor" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "NL", "Clara Cole", "Reviewer" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "NL", "Daan Dekker", "Reviewer" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "BE", "Emma Evers", "Editor" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "BE", "Finn Fontaine", "Reviewer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorId",
                table: "AuditEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_RecipientId",
                table: "AuditEntries",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_SubmissionId",
                table: "AuditEntries",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_EditorId",
                table: "Submissions",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_RecipientId",
                table: "Submissions",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ReviewerId",
                table: "Submissions",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "DocumentContents");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Recipients");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
