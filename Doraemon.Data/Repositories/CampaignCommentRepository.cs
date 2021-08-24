using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class CampaignCommentRepository : Repository
    {
        private const string DefaultApprovalMessage = "I approve of this campaign.";
        private const string DefaultOpposalMessage = "I do not approve of this campaign.";

        public CampaignCommentRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CampaignComment"/> with the specified <see cref="CampaignCommentCreationData"/>.
        /// </summary>
        /// <param name="data">The data needed to construct a new <see cref="CampaignComment"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null.</exception>
        public async Task CreateAsync(CampaignCommentCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.CampaignComments.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Returns if the user has already voted for approval or disapproval on the campaign provided.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="campaignId">The ID of the campaign.</param>
        /// <returns>A <see cref="bool"/> representing of the user has voted or not.</returns>
        public async Task<bool> HasUserAlreadyVoted(Snowflake userId, string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.AuthorId == userId)
                    .Where(x => x.CampaignId == campaignId)
                    .Where(x => x.Content == DefaultApprovalMessage || x.Content == DefaultOpposalMessage)
                    .AsNoTracking()
                    .AnyAsync();
            }
        }

        /// <summary>
        /// Fetches all comments for the given campaign's ID.
        /// </summary>
        /// <param name="campaignId">The <see cref="Campaign"/>'s ID that comments should be fetched from.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains all comments relating to this campaign.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchAllAsync(string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.CampaignId == campaignId)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Fetches a list of <see cref="CampaignComment"/> that expresses approval.
        /// </summary>
        /// <param name="campaignId">The <see cref="Campaign"/>'s ID that approvals should be fetched from.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that expresses approval of this campaign.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchApprovalsAsync(string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.CampaignId == campaignId)
                    .Where(x => x.Content.Equals(DefaultApprovalMessage, StringComparison.OrdinalIgnoreCase))
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Fetches a list of comments that are custom made, as in, not being default approvals or default opposals.
        /// </summary>
        /// <param name="campaignId">The <see cref="Campaign"/>'s ID that custom comments should be fetched from.</param>
        /// <returns></returns>
        public async Task<IEnumerable<CampaignComment>> FetchCustomCommentsAsync(string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.CampaignId == campaignId)
                    .Where(x => x.Content != DefaultApprovalMessage)
                    .Where(x => x.Content != DefaultOpposalMessage)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        
        /// <summary>
        /// Fetches a list of opposals for the campaign ID provided.
        /// </summary>
        /// <param name="campaignId">The <see cref="Campaign"/>'s ID value to fetch opposals from.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains a list of opposals for the given campaign.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchOpposalsAsync(string campaignId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.CampaignId == campaignId)
                    .Where(x => x.Content.Equals(DefaultOpposalMessage, StringComparison.OrdinalIgnoreCase))
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        
        /// <summary>
        /// Deletes the campaign comments provided.
        /// </summary>
        /// <param name="comments">The comments to be deleted.</param>
        public async Task DeleteAllAsync(IEnumerable<CampaignComment> comments)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.CampaignComments.RemoveRange(comments);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Checks if a the campaign comment already exists for the given Campaign Id.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <param name="content">The content to check for.</param>
        /// <returns>A <see cref="bool"/> representing if the message was found.</returns>
        public async Task<bool> FetchCommentsByContentAsync(string campaignId, string content)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.CampaignComments
                    .Where(x => x.CampaignId == campaignId)
                    .Where(x => x.Content.Equals(content, StringComparison.OrdinalIgnoreCase))
                    .AsNoTracking()
                    .AnyAsync();
            }
        }
    }
}