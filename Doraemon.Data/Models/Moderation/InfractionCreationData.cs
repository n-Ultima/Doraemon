using System;
using Disqord;

namespace Doraemon.Data.Models.Moderation
{
    /// <summary>
    ///     Describes an operation to create an instance of an <see cref="Infraction" />.
    /// </summary>
    public class InfractionCreationData
    {
        /// <summary>
        ///     See <see cref="Infraction.Id" />
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     See <see cref="Infraction.SubjectId" />
        /// </summary>
        public Snowflake SubjectId { get; set; }

        /// <summary>
        ///     See <see cref="Infraction.ModeratorId" />
        /// </summary>
        public Snowflake ModeratorId { get; set; }

        /// <summary>
        ///     See <see cref="Infraction.CreatedAt" />
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        /// <summary>
        ///     See <see cref="Infraction.Duration" />
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        ///     See <see cref="Infraction.Type" />
        /// </summary>
        public InfractionType Type { get; set; }

        /// <summary>
        ///     See <see cref="Infraction.Reason" />
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        ///     Converts the <see cref="InfractionCreationData" /> to a <see cref="Infraction" />
        /// </summary>
        /// <returns>A see <see cref="Infraction" /></returns>
        internal Infraction ToEntity()
        {
            return new()
            {
                Id = Id,
                SubjectId = SubjectId,
                ModeratorId = ModeratorId,
                CreatedAt = CreatedAt,
                Duration = Duration,
                Type = Type,
                Reason = Reason
            };
        }
    }
}