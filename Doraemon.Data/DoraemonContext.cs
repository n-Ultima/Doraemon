using System.Data.SqlTypes;
using Disqord;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Doraemon.Data
{
    // Declare that we inherit from DbContext
    public class DoraemonContext : DbContext
    {
        // Declare the connection string
        public DoraemonContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var snowflakeConverter = new ValueConverter<Snowflake, ulong>(static snowflake => snowflake, static @ulong => new Snowflake(@ulong));
            modelBuilder.UseValueConverterForType<Snowflake>(snowflakeConverter);
        }

        // Declare the Infractions Table
        public DbSet<Infraction> Infractions { get; set; }

        // Declare the PingRoles table
        public DbSet<PingRole> PingRoles { get; set; }

        // Declare the Tag table
        public DbSet<Tag> Tags { get; set; }

        // Declare the Guilds Table
        public DbSet<Guild> Guilds { get; set; }

        // Declare the Promotions Table
        public DbSet<Campaign> Campaigns { get; set; }

        // Declare the comments table
        public DbSet<CampaignComment> CampaignComments { get; set; }

        // Declare ModmailTicket Table
        public DbSet<ModmailTicket> ModmailTickets { get; set; }
        public DbSet<ModmailMessage> ModmailMessages { get; set; }

        // Declare the claim map table.
        public DbSet<RoleClaimMap> RoleClaimMaps { get; set; }
        
        public DbSet<UserClaimMap> UserClaimMaps { get; set; }
        
        /// <summary>
        /// Declare the guild users table
        /// </summary>
        public DbSet<GuildUser> GuildUsers { get; set; }
        
        /// <summary>
        /// Declare the punishment escalations table
        /// </summary>
        public DbSet<PunishmentEscalationConfiguration> PunishmentEscalationConfigurations { get; set; }
    }
}