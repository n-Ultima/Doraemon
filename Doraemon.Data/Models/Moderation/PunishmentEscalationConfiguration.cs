using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models.Moderation
{
    public class PunishmentEscalationConfiguration
    {
        /// <summary>
        /// The ID of this configuration.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        /// The number of infractions needed for the action to trigger.
        /// </summary>
        public int NumberOfInfractionsToTrigger { get; set; }
        /// <summary>
        /// The type of infraction that will be applied.
        /// </summary>
        public InfractionType Type { get; set; }
        /// <summary>
        /// The duration of the infraction.
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}