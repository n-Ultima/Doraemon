using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class UserClaims : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RoleId",
                table: "ClaimMaps",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "UserId",
                table: "ClaimMaps",
                type: "numeric(20,0)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ClaimMaps");

            migrationBuilder.AlterColumn<decimal>(
                name: "RoleId",
                table: "ClaimMaps",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);
        }
    }
}
