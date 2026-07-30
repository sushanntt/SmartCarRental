using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HajurKoCarRental.Migrations
{
    public partial class AddCarIdToLateFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE LateFees DROP FOREIGN KEY IF EXISTS FK_LateFees_Car_CarId");
            migrationBuilder.Sql("DROP INDEX IX_LateFees_CarId ON LateFees");
            migrationBuilder.Sql("ALTER TABLE LateFees DROP COLUMN IF EXISTS CarId");

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "LateFees",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_LateFees_CarId",
                table: "LateFees",
                column: "CarId");

            migrationBuilder.AddForeignKey(
                name: "FK_LateFees_Car_CarId",
                table: "LateFees",
                column: "CarId",
                principalTable: "Car",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LateFees_Car_CarId",
                table: "LateFees");

            migrationBuilder.DropIndex(
                name: "IX_LateFees_CarId",
                table: "LateFees");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "LateFees");
        }
    }
}
