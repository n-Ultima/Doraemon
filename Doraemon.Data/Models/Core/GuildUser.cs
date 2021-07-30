using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Core
{
    public class GuildUser
    {
        /// <summary>
        /// The ID value of the user.
        /// </summary>
        public Snowflake Id { get; set; }

        /// <summary>
        /// The Username assigned to the user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The Discriminator assigned to the user.
        /// </summary>
        public string Discriminator { get; set; }

        /// <summary>
        /// A boolean representing if the user can interact with modmail.
        /// </summary>
        public bool IsModmailBlocked { get; set; }
    }

    public class GuildUserConfigurator : IEntityTypeConfiguration<GuildUser>
    {
        public void Configure(EntityTypeBuilder<GuildUser> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.Id)
                .HasConversion<ulong>();
        }
    }
}