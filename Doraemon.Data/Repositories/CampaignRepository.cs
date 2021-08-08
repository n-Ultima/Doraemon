using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class CampaignRepository : RepositoryVersionTwo
    {
        public CampaignRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Campaign"/> with the specified <see cref="CampaignCreationData"/>.
        /// </summary>
        /// <param name="data">The <see cref="CampaignCreationData"/> needed to construct a new <see cref="Campaign"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null.</exception>
        public async Task CreateAsync(CampaignCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.Campaigns.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Fetches a campaign by the user ID provided.
        /// </summary>
        /// <param name="userId">The ID value of the user who has an ongoing campaign.</param>
        /// <returns>A <see cref="Campaign"/> if the user is involved in one.</returns>
        public async Task<Campaign> FetchCampaignByUserIdAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Campaigns
                    .Where(x => x.UserId == userId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Fetches all ongoing campaigns.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Campaign}"/> that contains all ongoing campaigns.</returns>
        public async Task<IEnumerable<Campaign>> FetchAllAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Campaigns
                    .AsQueryable()
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Fetches a campaign with the given ID.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign to query for.</param>
        /// <returns>A <see cref="Campaign"/> with the matching ID.</returns>
        public async Task<Campaign> FetchAsync(string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Campaigns
                    .FindAsync(campaignId);
            }
        }

        /// <summary>
        /// Deletes the campaign provided.
        /// </summary>
        /// <param name="campaign">The <see cref="Campaign"/> to delete from the table.</param>
        public async Task DeleteAsync(Campaign campaign)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.Campaigns.Remove(campaign);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}