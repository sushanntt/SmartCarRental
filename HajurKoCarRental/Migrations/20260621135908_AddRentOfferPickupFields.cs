using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HajurKoCarRental.Migrations
{
    public partial class AddRentOfferPickupFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "RentOffer",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "PickupBranchId",
                table: "RentOffer",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<double>(
                name: "PickupLat",
                table: "RentOffer",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLng",
                table: "RentOffer",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "RentOffer",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SelectedColor",
                table: "RentOffer",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RentOffer",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RentOffer_PickupBranchId",
                table: "RentOffer",
                column: "PickupBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_RentOffer_Branches_PickupBranchId",
                table: "RentOffer",
                column: "PickupBranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentOffer_Branches_PickupBranchId",
                table: "RentOffer");

            migrationBuilder.DropIndex(
                name: "IX_RentOffer_PickupBranchId",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "PickupBranchId",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "PickupLng",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "SelectedColor",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RentOffer");
        }
    }
}
