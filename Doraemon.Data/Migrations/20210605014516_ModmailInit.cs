using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class ModmailInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "ModmailTickets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModmailChannel = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DmChannel = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModmailTickets", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "ModmailTickets");

        }
    }
}
