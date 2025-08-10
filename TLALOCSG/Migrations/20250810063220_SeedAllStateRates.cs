using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TLALOCSG.Migrations
{
    /// <inheritdoc />
    public partial class SeedAllStateRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StateRates",
                columns: new[] { "StateCode", "DistanceKm", "ShipPerKm", "StateName", "TransportPerKm" },
                values: new object[,]
                {
                    { "AGS", 180, 6m, "Aguascalientes", 10m },
                    { "BCN", 2300, 6m, "Baja California", 10m },
                    { "BCS", 1600, 6m, "Baja California Sur", 10m },
                    { "CAM", 1350, 6m, "Campeche", 10m },
                    { "CHH", 1160, 6m, "Chihuahua", 10m },
                    { "CHP", 1100, 6m, "Chiapas", 10m },
                    { "COA", 800, 6m, "Coahuila", 10m },
                    { "COL", 470, 6m, "Colima", 10m },
                    { "DUR", 600, 6m, "Durango", 10m },
                    { "GRO", 650, 6m, "Guerrero", 10m },
                    { "HGO", 400, 6m, "Hidalgo", 10m },
                    { "MEX", 280, 6m, "Estado de México", 10m },
                    { "MIC", 200, 6m, "Michoacán", 10m },
                    { "MOR", 420, 6m, "Morelos", 10m },
                    { "NAY", 460, 6m, "Nayarit", 10m },
                    { "OAX", 740, 6m, "Oaxaca", 10m },
                    { "PUE", 520, 6m, "Puebla", 10m },
                    { "QRO", 130, 6m, "Querétaro", 10m },
                    { "ROO", 1800, 6m, "Quintana Roo", 10m },
                    { "SIN", 820, 6m, "Sinaloa", 10m },
                    { "SLP", 220, 6m, "San Luis Potosí", 10m },
                    { "SON", 1200, 6m, "Sonora", 10m },
                    { "TAB", 1050, 6m, "Tabasco", 10m },
                    { "TAM", 800, 6m, "Tamaulipas", 10m },
                    { "TLA", 520, 6m, "Tlaxcala", 10m },
                    { "VER", 650, 6m, "Veracruz", 10m },
                    { "YUC", 1550, 6m, "Yucatán", 10m },
                    { "ZAC", 350, 6m, "Zacatecas", 10m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "AGS");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "BCN");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "BCS");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "CAM");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "CHH");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "CHP");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "COA");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "COL");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "DUR");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "GRO");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "HGO");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "MEX");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "MIC");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "MOR");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "NAY");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "OAX");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "PUE");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "QRO");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "ROO");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "SIN");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "SLP");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "SON");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "TAB");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "TAM");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "TLA");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "VER");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "YUC");

            migrationBuilder.DeleteData(
                table: "StateRates",
                keyColumn: "StateCode",
                keyValue: "ZAC");
        }
    }
}
