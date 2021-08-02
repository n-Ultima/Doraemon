using System;
using Disqord;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Moderation
{
    public class Infraction
    {
        /// <summary>
        ///     The moderator that issued the infraction.
        /// </summary>
        public Snowflake ModeratorId { get; set; }

        /// <summary>
        ///     The user that is being issued the infraction.
        /// </summary>
        public Snowflake SubjectId { get; set; }

        /// <summary>
        ///     The ID of the infraction.
        /// </summary>
        public string Id { get; set; } // Define as a string so we can convert a GUID into a string.

        /// <summary>
        ///     The reason for the infraction being given.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        ///     The type of infraction.
        /// </summary>
        public InfractionType Type { get; set; }

        /// <summary>
        ///     When the infraction was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsEscalation { get; set; }
        /// <summary>
        ///     The duration of the infraction.
        /// </summary>
        public TimeSpan? Duration { get; set; }
        
        /// <summary>
        /// When the infraction should be rescinded.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }
    }
    public class InfractionConfigurator : IEntityTypeConfiguration<Infraction>
    {
        public void Configure(EntityTypeBuilder<Infraction> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.SubjectId)
                .HasConversion<ulong>();
            entityTypeBuilder
                .Property(x => x.ModeratorId)
                .HasConversion<ulong>();
        }
    }
}