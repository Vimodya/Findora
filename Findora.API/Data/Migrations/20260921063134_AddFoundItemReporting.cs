using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Findora.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFoundItemReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoundItemReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DateFound = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApproximateTimeFound = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
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
                    table.PrimaryKey("PK_FoundItemReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoundItemReports_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoundItemReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_CategoryId",
                table: "FoundItemReports",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_DateFound",
                table: "FoundItemReports",
                column: "DateFound");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_Status",
                table: "FoundItemReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_UserId",
                table: "FoundItemReports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundItemReports");
        }
    }
}
