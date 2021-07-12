﻿// <auto-generated />
using System;
using Doraemon.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Doraemon.Data.Migrations
{
    [DbContext(typeof(DoraemonContext))]
    [Migration("20210711235958_PunishmentEscalationsConfigurationAdd")]
    partial class PunishmentEscalationsConfigurationAdd
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.6")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Doraemon.Data.Models.Core.ClaimMap", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<decimal?>("RoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<decimal?>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("ClaimMaps");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Core.Guild", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Core.GuildUser", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Discriminator")
                        .HasColumnType("text");

                    b.Property<bool>("IsModmailBlocked")
                        .HasColumnType("boolean");

                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("GuildUsers");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Moderation.Infraction", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("interval");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<decimal>("SubjectId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Infractions");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Moderation.ModmailTicket", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("DmChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModmailChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("ModmailTickets");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Moderation.PunishmentEscalationConfiguration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("interval");

                    b.Property<int>("NumberOfInfractionsToTrigger")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("PunishmentEscalationConfigurations");
                });

            modelBuilder.Entity("Doraemon.Data.Models.PingRole", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("PingRoles");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Promotion.Campaign", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("InitiatorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("ReasonForCampaign")
                        .HasColumnType("text");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Campaigns");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Promotion.CampaignComment", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("AuthorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("CampaignId")
                        .HasColumnType("text");

                    b.Property<string>("Content")
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("CampaignComments");
                });

            modelBuilder.Entity("Doraemon.Data.Models.Tag", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("citext");

                    b.Property<decimal>("OwnerId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Response")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });
#pragma warning restore 612, 618
        }
    }
}
