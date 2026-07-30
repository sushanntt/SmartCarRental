using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HajurKoCarRental.Migrations
{
    public partial class AddRentalHistoryPickupBranch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PickupBranchId",
                table: "RentalHistory",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_RentalHistory_PickupBranchId",
                table: "RentalHistory",
                column: "PickupBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalHistory_Branches_PickupBranchId",
                table: "RentalHistory",
                column: "PickupBranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalHistory_Branches_PickupBranchId",
                table: "RentalHistory");

            migrationBuilder.DropIndex(
                name: "IX_RentalHistory_PickupBranchId",
                table: "RentalHistory");

            migrationBuilder.DropColumn(
                name: "PickupBranchId",
                table: "RentalHistory");
        }
    }
}
