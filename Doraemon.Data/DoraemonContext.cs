
using System;
using Doraemon.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.IO;
using System.Reflection;
namespace Doraemon.Data
{
    // Declare that we inherit from DbContext
    public class DoraemonContext : DbContext
    {
        // Declare the Infractions Table
        public DbSet<Infraction> Infractions { get; set; }
        // Declare the Roles table
        public DbSet<Role> Roles { get; set; }
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
        // Declare the connection string
        public DoraemonContext(DbContextOptions options) : base(options)
        { }
    }
}