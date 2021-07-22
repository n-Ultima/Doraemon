using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class CampaignRepository : Repository
    {
        public CampaignRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {
        }

        public async Task CreateAsync(CampaignCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.Campaigns.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }


        public async Task<Campaign> FetchCampaignByUserIdAsync(ulong userId)
        {
            return await DoraemonContext.Campaigns
                .Where(x => x.UserId == userId)
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<Campaign>> FetchAllAsync()
        {
            return await DoraemonContext.Campaigns.AsQueryable().ToListAsync();
        }

        public async Task<Campaign> FetchAsync(string campaignId)
        {
            return await DoraemonContext.Campaigns
                .FindAsync(campaignId);
        }

        public async Task DeleteAsync(Campaign campaign)
        {
            DoraemonContext.Campaigns.Remove(campaign);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}