using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class RemoveModmailBlockProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsModmailBlocked",
                table: "GuildUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsModmailBlocked",
                table: "GuildUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
