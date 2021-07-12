using System;

namespace Doraemon.Data.Models.Moderation
{
    public class PunishmentEscalationConfigurationCreationData
    {
        /// <summary>
        /// See <see cref="PunishmentEscalationConfiguration.NumberOfInfractionsToTrigger"/>
        /// </summary>
        public int NumberOfInfractionsToTrigger { get; set; }
        /// <summary>
        /// See <see cref="PunishmentEscalationConfiguration.Type"/>
        /// </summary>
        public InfractionType Type { get; set; }
        
        /// <summary>
        /// See <see cref="PunishmentEscalationConfiguration.Duration"/>
        /// </summary>
        public TimeSpan? Duration { get; set; }

        internal PunishmentEscalationConfiguration ToEntity()
            => new PunishmentEscalationConfiguration()
            {
                NumberOfInfractionsToTrigger = NumberOfInfractionsToTrigger,
                Type = Type,
                Duration = Duration
            };
    }
}