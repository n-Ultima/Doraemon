
using System;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Promotion;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Models.Core;

namespace Doraemon.Data
{
    // Declare that we inherit from DbContext
    public class DoraemonContext : DbContext
    {
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
        // Declare the comments
        public DbSet<CampaignComment> CampaignComments { get; set; }
        // Declare ModmailTicket
        public DbSet<ModmailTicket> ModmailTickets { get; set; }
        public DbSet<ClaimMap> ClaimMaps { get; set; }
        public DbSet<GuildUser> GuildUsers { get; set; }
        // Declare the connection string
        public DoraemonContext(DbContextOptions options) : base(options)
        { }
    }
}