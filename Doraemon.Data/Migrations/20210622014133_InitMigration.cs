using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Doraemon.Data.Migrations
{
    public partial class InitMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CampaignId = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "citext", nullable: true),
                    AuthorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    InitiatorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ReasonForCampaign = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildRoles",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Infractions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SubjectId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Infractions", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "PingRoles",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: true),
                    Response = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignComments");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "ClaimMaps");

            migrationBuilder.DropTable(
                name: "GuildRoles");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Infractions");

            migrationBuilder.DropTable(
                name: "ModmailTickets");

            migrationBuilder.DropTable(
                name: "PingRoles");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
