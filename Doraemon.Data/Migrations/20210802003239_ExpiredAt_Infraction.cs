using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class ExpiredAt_Infraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "Infractions",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Infractions");
        }
    }
}
