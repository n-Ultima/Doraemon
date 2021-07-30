using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailTicket
    {
        /// <summary>
        ///     I only declare the ID here to satisfy Ef core.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The user's ID who contacted Modmail
        /// </summary>
        public Snowflake UserId { get; set; }

        /// <summary>
        ///     The channel inside of the guild that Staff will use to respond to the ticket.
        /// </summary>
        public Snowflake ModmailChannelId { get; set; }

        /// <summary>
        ///     The corresponding DM channel.
        /// </summary>
        public Snowflake DmChannelId { get; set; }
        
    }
    public class ModmailTicketConfigurator : IEntityTypeConfiguration<ModmailTicket>
    {
        public void Configure(EntityTypeBuilder<ModmailTicket> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.UserId)
                .HasConversion<ulong>();
            entityTypeBuilder
                .Property(x => x.ModmailChannelId)
                .HasConversion<ulong>();
            entityTypeBuilder
                .Property(x => x.DmChannelId)
                .HasConversion<ulong>();
        }
    }
}