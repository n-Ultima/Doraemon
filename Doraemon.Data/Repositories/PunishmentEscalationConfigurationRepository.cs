using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Moderation;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class PunishmentEscalationConfigurationRepository : RepositoryVersionTwo
    {
        public PunishmentEscalationConfigurationRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Creates a new <see cref="PunishmentEscalationConfiguration"/> with the specified <see cref="PunishmentEscalationConfigurationCreationData"/>.
        /// </summary>
        /// <param name="data">The <see cref="PunishmentEscalationConfigurationCreationData"/></param> needed to construct a new <see cref="PunishmentEscalationConfiguration"/>.
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null.</exception>
        public async Task CreateAsync(PunishmentEscalationConfigurationCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.PunishmentEscalationConfigurations.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Fetches a punishment configuration.
        /// </summary>
        /// <param name="amount">The amount of punishments needed for this configuration to take effect.</param>
        /// <param name="type">The type of infraction that will be applied upon reaching the <see cref="amount"/>.</param>
        /// <returns>A <see cref="PunishmentEscalationConfiguration"/> if it exists.</returns>
        public async Task<PunishmentEscalationConfiguration> FetchAsync(int amount, InfractionType type)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.PunishmentEscalationConfigurations
                    .Where(x => x.NumberOfInfractionsToTrigger == amount)
                    .Where(x => x.Type == type)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Fetches a punishment configuration.
        /// </summary>
        /// <param name="amount">The amount of punishments needed for this configuration to take effect.</param>
        /// <returns>A <see cref="PunishmentEscalationConfiguration"/> that triggers when the <see cref="amount"/> of punishments is reached. Returns null otherwise.</returns>
        public async Task<PunishmentEscalationConfiguration> FetchAsync(int amount)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.PunishmentEscalationConfigurations
                    .Where(x => x.NumberOfInfractionsToTrigger == amount)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Updates an already-existing punishment configuration.
        /// </summary>
        /// <param name="punishmentConfig">The <see cref="PunishmentEscalationConfiguration"/> to modify.</param>
        /// <param name="updatedType">The optional updated type of the <see cref="punishmentConfig"/>.</param>
        /// <param name="updatedDuration">The optional updated duration of the <see cref="punishmentConfig"/>.</param>
        public async Task UpdateAsync(PunishmentEscalationConfiguration punishmentConfig, InfractionType? updatedType, TimeSpan? updatedDuration)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                if (!updatedDuration.HasValue && !updatedType.HasValue)
                {
                    return;
                }

                if (updatedDuration.HasValue && !updatedType.HasValue)
                {
                    punishmentConfig.Duration = updatedDuration;
                    await doraemonContext.SaveChangesAsync();
                    return;
                }

                if (!updatedDuration.HasValue && updatedType.HasValue)
                {
                    punishmentConfig.Type = updatedType.Value;
                    await doraemonContext.SaveChangesAsync();
                }

                if (updatedDuration.HasValue && updatedType.HasValue)
                {
                    punishmentConfig.Type = updatedType.Value;
                    punishmentConfig.Duration = updatedDuration;
                    await doraemonContext.SaveChangesAsync();
                }
            }

        }

        /// <summary>
        /// Deletes a <see cref="PunishmentEscalationConfiguration"/> from the database.
        /// </summary>
        /// <param name="config">The <see cref="PunishmentEscalationConfiguration"/> to delete.</param>
        public async Task DeleteAsync(PunishmentEscalationConfiguration config)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.PunishmentEscalationConfigurations.Remove(config);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}