using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class CampaignCommentRepository : Repository
    {
        public const string DefaultApprovalMessage = "I approve of this campaign.";
        public const string DefaultOpposalMessage = "I do not approve of this campaign.";

        public CampaignCommentRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {
        }
        private static readonly RepositoryTransactionFactory _createTransactionFactory = new RepositoryTransactionFactory();
        public Task<IRepositoryTransaction> BeginCreateTransactionAsync()
            => _createTransactionFactory.BeginTransactionAsync(DoraemonContext.Database);

        public async Task CreateAsync(CampaignCommentCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.CampaignComments.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        public async Task<bool> HasUserAlreadyVoted(ulong userId, string campaignId)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.AuthorId == userId)
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == DefaultApprovalMessage || x.Content == DefaultOpposalMessage)
                .AsNoTracking()
                .AnyAsync();
        }

        public async Task<IEnumerable<CampaignComment>> FetchAllAsync(string campaignId)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.CampaignId == campaignId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<CampaignComment>> FetchApprovalsAsync(string campaignId)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == DefaultApprovalMessage)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<CampaignComment>> FetchCustomCommentsAsync(string campaignId)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content != DefaultApprovalMessage)
                .Where(x => x.Content != DefaultOpposalMessage)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<CampaignComment>> FetchOpposalsAsync(string campaignId)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == DefaultOpposalMessage)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task DeleteAllAsync(IEnumerable<CampaignComment> comments)
        {
            DoraemonContext.CampaignComments.RemoveRange(comments);
            await DoraemonContext.SaveChangesAsync();
        }

        public async Task<bool> FetchCommentsByContentAsync(string campaignId, string content)
        {
            return await DoraemonContext.CampaignComments
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == content)
                .AsNoTracking()
                .AnyAsync();
        }
    }
}