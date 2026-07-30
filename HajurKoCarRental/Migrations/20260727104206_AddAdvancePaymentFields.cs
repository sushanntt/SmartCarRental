using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HajurKoCarRental.Migrations
{
    public partial class AddAdvancePaymentFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AdvancePaid",
                table: "RentOffer",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RemainingAmount",
                table: "RentOffer",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AdvancePaid",
                table: "RentalHistory",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RemainingAmount",
                table: "RentalHistory",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AdvancePaid",
                table: "Rent",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RemainingAmount",
                table: "Rent",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvancePaid",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "RentOffer");

            migrationBuilder.DropColumn(
                name: "AdvancePaid",
                table: "RentalHistory");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "RentalHistory");

            migrationBuilder.DropColumn(
                name: "AdvancePaid",
                table: "Rent");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "Rent");
        }
    }
}
