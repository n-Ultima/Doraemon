using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Moderation
{
    /// <summary>
    /// Describes an operation to create an instance of an <see cref="Infraction"/>.
    /// </summary>
    public class InfractionCreationData
    {
        /// <summary>
        /// See <see cref="Infraction.Id"/>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// See <see cref="Infraction.SubjectId"/>
        /// </summary>
        public ulong SubjectId { get; set; }

        /// <summary>
        /// See <see cref="Infraction.ModeratorId"/>
        /// </summary>
        public ulong ModeratorId { get; set; }

        /// <summary>
        /// See <see cref="Infraction.CreatedAt"/>
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// See <see cref="Infraction.Duration"/>
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// See <see cref="Infraction.Type"/>
        /// </summary>
        public InfractionType Type { get; set; }

        /// <summary>
        /// See <see cref="Infraction.Reason"/>
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Converts the <see cref="InfractionCreationData"/> to a <see cref="Infraction"/>
        /// </summary>
        /// <returns>A see <see cref="Infraction"/></returns>
        internal Infraction ToEntity()
            => new Infraction()
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
