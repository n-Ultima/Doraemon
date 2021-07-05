using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class NamingFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModmailChannel",
                table: "ModmailTickets",
                newName: "ModmailChannelId");

            migrationBuilder.RenameColumn(
                name: "DmChannel",
                table: "ModmailTickets",
                newName: "DmChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModmailChannelId",
                table: "ModmailTickets",
                newName: "ModmailChannel");

            migrationBuilder.RenameColumn(
                name: "DmChannelId",
                table: "ModmailTickets",
                newName: "DmChannel");
        }
    }
}
