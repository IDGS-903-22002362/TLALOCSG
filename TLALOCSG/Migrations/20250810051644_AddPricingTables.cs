using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TLALOCSG.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstallTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinQty = table.Column<int>(type: "int", nullable: false),
                    MaxQty = table.Column<int>(type: "int", nullable: true),
                    BaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateRates",
                columns: table => new
                {
                    StateCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StateName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DistanceKm = table.Column<int>(type: "int", nullable: false),
                    ShipPerKm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransportPerKm = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateRates", x => x.StateCode);
                });

            migrationBuilder.InsertData(
                table: "InstallTiers",
                columns: new[] { "Id", "BaseCost", "MaxQty", "MinQty" },
                values: new object[,]
                {
                    { 1, 2000m, 5, 1 },
                    { 2, 5500m, 15, 6 },
                    { 3, 9000m, null, 16 }
                });

            migrationBuilder.InsertData(
                table: "StateRates",
                columns: new[] { "StateCode", "DistanceKm", "ShipPerKm", "StateName", "TransportPerKm" },
                values: new object[,]
                {
                    { "CDMX", 330, 6m, "Ciudad de México", 10m },
                    { "GTO", 0, 0m, "Guanajuato", 0m },
                    { "JAL", 280, 6m, "Jalisco", 10m },
                    { "NLE", 700, 6m, "Nuevo León", 10m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstallTiers_Range",
                table: "InstallTiers",
                columns: new[] { "MinQty", "MaxQty" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstallTiers");

            migrationBuilder.DropTable(
                name: "StateRates");
        }
    }
}
