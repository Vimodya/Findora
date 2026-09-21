using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Findora.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLostItemReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LostItemReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DateLost = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApproximateTimeLost = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    LocationDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IdentifyingCharacteristics = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    ContactPreference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Platform"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostItemReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LostItemReports_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LostItemReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ItemCategories",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Electronics", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Documents", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Clothing", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Bags", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Keys", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Jewelry", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Books", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Pets", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Vehicles", null },
                    { new Guid("d1a1c2b0-0001-4a10-9c10-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Other", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_Name",
                table: "ItemCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LostItemReports_CategoryId",
                table: "LostItemReports",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LostItemReports_DateLost",
                table: "LostItemReports",
                column: "DateLost");

            migrationBuilder.CreateIndex(
                name: "IX_LostItemReports_Status",
                table: "LostItemReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LostItemReports_UserId",
                table: "LostItemReports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LostItemReports");

            migrationBuilder.DropTable(
                name: "ItemCategories");
        }
    }
}
