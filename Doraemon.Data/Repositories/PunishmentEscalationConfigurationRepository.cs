using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Moderation;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class PunishmentEscalationConfigurationRepository : Repository
    {
        public PunishmentEscalationConfigurationRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {
        }

        public async Task CreateAsync(PunishmentEscalationConfigurationCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.PunishmentEscalationConfigurations.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        public async Task<PunishmentEscalationConfiguration> FetchAsync(int amount, InfractionType type)
        {
            return await DoraemonContext.PunishmentEscalationConfigurations
                .Where(x => x.NumberOfInfractionsToTrigger == amount)
                .Where(x => x.Type == type)
                .SingleOrDefaultAsync();
        }

        public async Task<PunishmentEscalationConfiguration> FetchAsync(int amount)
        {
            return await DoraemonContext.PunishmentEscalationConfigurations
                .Where(x => x.NumberOfInfractionsToTrigger == amount)
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<PunishmentEscalationConfiguration>> FetchAllAsync(int amount)
        {
            return await DoraemonContext.PunishmentEscalationConfigurations
                .Where(x => x.NumberOfInfractionsToTrigger == amount)
                .ToListAsync();
        }

        public async Task UpdateAsync(PunishmentEscalationConfiguration punishmentConfig, InfractionType? updatedType, TimeSpan? updatedDuration)
        {
            if (!updatedDuration.HasValue && !updatedType.HasValue)
            {
                return;
            }

            if (updatedDuration.HasValue && !updatedType.HasValue)
            {
                punishmentConfig.Duration = updatedDuration;
                await DoraemonContext.SaveChangesAsync();
                return;
            }

            if (!updatedDuration.HasValue && updatedType.HasValue)
            {
                punishmentConfig.Type = updatedType.Value;
                await DoraemonContext.SaveChangesAsync();
            }

            if (updatedDuration.HasValue && updatedType.HasValue)
            {
                punishmentConfig.Type = updatedType.Value;
                punishmentConfig.Duration = updatedDuration;
            }
                
        }
    }
}