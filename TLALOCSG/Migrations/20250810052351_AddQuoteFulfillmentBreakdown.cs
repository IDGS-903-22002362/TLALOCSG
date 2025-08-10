using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TLALOCSG.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteFulfillmentBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fulfillment",
                table: "Quotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallBase",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "Quotes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProducts",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportCost",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fulfillment",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InstallBase",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TotalProducts",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TransportCost",
                table: "Quotes");
        }
    }
}
