using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TLALOCSG.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Quotes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Quotes");
        }
    }
}
