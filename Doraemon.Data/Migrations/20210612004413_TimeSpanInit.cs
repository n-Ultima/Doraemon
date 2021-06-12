using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Doraemon.Data.Migrations
{
    public partial class TimeSpanInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Infractions",
                type: "interval",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Infractions");
        }
    }
}
