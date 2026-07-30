using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HajurKoCarRental.Migrations
{
    public partial class RemoveColorFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarBranchColors");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "RentalHistory");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "Rent");

            migrationBuilder.DropColumn(
                name: "AvailableColors",
                table: "Offer");

            migrationBuilder.DropColumn(
                name: "AvailableColors",
                table: "Car");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Car");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "RentOffer",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "RentalHistory",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "Rent",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AvailableColors",
                table: "Offer",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AvailableColors",
                table: "Car",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Car",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CarBranchColors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CarBranchId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AvailableCount = table.Column<int>(type: "int", nullable: false),
                    ColorName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarBranchColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarBranchColors_CarBranches_CarBranchId",
                        column: x => x.CarBranchId,
                        principalTable: "CarBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CarBranchColors_CarBranchId",
                table: "CarBranchColors",
                column: "CarBranchId");
        }
    }
}
