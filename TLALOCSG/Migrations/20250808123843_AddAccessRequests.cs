using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TLALOCSG.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_Email",
                table: "AccessRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_Email_Status",
                table: "AccessRequests",
                columns: new[] { "Email", "Status" });
            migrationBuilder.Sql(
        "CREATE UNIQUE INDEX UX_AccessRequests_Email_Pending ON AccessRequests(Email) WHERE Status = 'Pending';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX UX_AccessRequests_Email_Pending ON AccessRequests;");
            migrationBuilder.DropTable(
                name: "AccessRequests");
        }
    }
}
