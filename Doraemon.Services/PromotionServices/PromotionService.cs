using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Promotion;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Server;

namespace Doraemon.Services.PromotionServices
{
    [DoraemonService]
    public class PromotionService : DoraemonBotService
    {
        private const string DefaultApprovalMessage = "I approve of this campaign.";
        private const string DefaultOpposalMessage = "I do not approve of this campaign.";
        private readonly AuthorizationService _authorizationService;

        public PromotionService(IServiceProvider serviceProvider, AuthorizationService authorizationService)
            : base(serviceProvider)
        {
            _authorizationService = authorizationService;
        }

        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        /// <summary>
        ///     Nominates a user to be promoted to the specified role in the <see cref="DoraemonConfiguration"/.>
        /// </summary>
        /// <param name="userId">The ID value of the user being nominated.</param>
        /// <param name="initiatorId">The ID value of the user initiating the campaign.</param>
        /// <param name="comment">The starting comment that the Initiator would like to leave regarding the campaign.</param>
        /// <param name="guildId">The guild that the campaign is happening in.</param>
        /// <param name="channelId">The channel ID that the campaign is launched in.</param>
        /// <returns></returns>
        public async Task NominateUserAsync(Snowflake userId, Snowflake initiatorId, string comment, Snowflake guildId,
            Snowflake channelId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionStart);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var promo = await campaignRepository.FetchCampaignByUserIdAsync(userId);
                if (promo is not null)
                    throw new Exception("There is already an ongoing campaign for this user.");
                var ID = DatabaseUtilities.ProduceId();
                await campaignRepository.CreateAsync(new CampaignCreationData
                {
                    Id = ID,
                    InitiatorId = initiatorId,
                    UserId = userId,
                    ReasonForCampaign = comment
                });


                var embed = new LocalEmbed()
                    .WithTitle("Campaign Started")
                    .WithDescription(
                        $"A campaign was started for <@{userId}>, with reason: `{comment}")
                    .WithColor(DColor.Purple)
                    .WithFooter("Goodluck!");
                var guild = Bot.GetGuild(guildId);
                var channel = guild.GetChannel(channelId) as ITextChannel;
                await channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
            }
        }

        /// <summary>
        ///     Adds a custom note to an ongoing campaign.
        /// </summary>
        /// <param name="authorId">The ID value of the author adding the note.</param>
        /// <param name="campaignId">The ID value of the campaign to add this note to.</param>
        /// <param name="note">The content of the note itself.</param>
        /// <returns></returns>
        public async Task AddNoteToCampaignAsync(Snowflake authorId, string campaignId, string note)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                var promo = await campaignRepository.FetchAsync(campaignId);
                if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
                var currentPromoNotes = await campaignCommentRepository.FetchCommentsByContentAsync(campaignId, note);
                if (currentPromoNotes)
                    throw new Exception(
                        "There is already an existing comment on the campaign provided that matches the Content provided.");
                await campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
                {
                    AuthorId = authorId,
                    CampaignId = campaignId,
                    Content = note
                });
            }
        }

        /// <summary>
        ///     Adds a note to the campaign that expresses approval.
        /// </summary>
        /// <param name="authorId">The ID value of the author.</param>
        /// <param name="campaignId">The campaign ID that the note will be applied to.</param>
        /// <returns></returns>
        public async Task ApproveCampaignAsync(Snowflake authorId, string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);

            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                var promo = await campaignRepository.FetchAsync(campaignId);
                var alreadyVoted = await campaignCommentRepository.HasUserAlreadyVoted(authorId, campaignId);
                if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
                if (alreadyVoted)
                    throw new Exception(
                        "You have already voted for the current campaign, so you cannot vote again.");

                await campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
                {
                    AuthorId = authorId,
                    CampaignId = campaignId,
                    Content = DefaultApprovalMessage
                });
            }
        }

        /// <summary>
        ///     Adds a note to a campaign that expresses opposal.
        /// </summary>
        /// <param name="authorId">The ID value of the author.</param>
        /// <param name="campaignId">The campaign ID to apply the note to.</param>
        /// <returns></returns>
        public async Task OpposeCampaignAsync(Snowflake authorId, string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                var promo = await campaignRepository.FetchAsync(campaignId);
                if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
                var alreadyVoted = await campaignCommentRepository.HasUserAlreadyVoted(authorId, campaignId);
                if (alreadyVoted)
                    throw new Exception(
                        "You have already voted for the current campaign, so you cannot vote again.");
                await campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
                {
                    AuthorId = authorId,
                    CampaignId = campaignId,
                    Content = DefaultOpposalMessage
                });
            }
        }

        /// <summary>
        ///     Rejects a campaign, denying it.
        /// </summary>
        /// <param name="campaignId">The ID of the campaign to reject.</param>
        /// <param name="guildId">The ID of the guild that the campaign originated from.</param>
        /// <returns></returns>
        public async Task RejectCampaignAsync(string campaignId, Snowflake guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                var promo = await campaignRepository.FetchAsync(campaignId);
                var promoComments = await campaignCommentRepository.FetchAllAsync(campaignId);
                if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
                await campaignRepository.DeleteAsync(promo);
                await campaignCommentRepository.DeleteAllAsync(promoComments);
            }
        }

        /// <summary>
        ///     Accepts a campaign, promoting the user.
        /// </summary>
        /// <param name="campaignId">The ID of the campaign.</param>
        /// <param name="guildId">The guild ID that the campaign originated from.</param>
        /// <returns></returns>
        public async Task AcceptCampaignAsync(string campaignId, Snowflake guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                var guild = Bot.GetGuild(guildId);
                var role = Bot.GetRole(guild.Id, DoraemonConfig.PromotionRoleId);
                var promo = await campaignRepository.FetchAsync(campaignId);
                var promoComments = await campaignCommentRepository.FetchAllAsync(campaignId);
                var user = guild.GetMember(promo.UserId);
                if (user is null)
                {
                    await campaignRepository.DeleteAsync(promo);
                    await campaignCommentRepository.DeleteAllAsync(promoComments);
                    throw new ArgumentException(
                        "The user involved in this campaign has left the server, thus, the campaign is automatically rejected.");
                }

                await user.GrantRoleAsync(role.Id);
                if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
                await campaignRepository.DeleteAsync(promo);
                await campaignCommentRepository.DeleteAllAsync(promoComments);
                var promotionLog = guild.GetChannel(DoraemonConfig.LogConfiguration.PromotionLogChannelId) as ITextChannel;
                var promoLogEmbed = new LocalEmbed()
                    .WithAuthor(user)
                    .WithTitle("The campaign is over!")
                    .WithDescription(
                        $"Staff accepted the campaign, and **{user.Tag}** was promoted to <@&{DoraemonConfig.PromotionRoleId}>!🎉")
                    .WithFooter("Congrats on the promotion!");
                await promotionLog.SendMessageAsync(new LocalMessage().WithEmbeds(promoLogEmbed));
            }
        }

        /// <summary>
        /// Fetches a list of custom comments(comments that aren't matching the <see cref="DefaultApprovalMessage"/> or the <see cref="DefaultOpposalMessage"/>.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/></returns>
        public async Task<IEnumerable<CampaignComment>> FetchCustomCommentsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                return await campaignCommentRepository.FetchCustomCommentsAsync(campaignId);

            }
        }

        /// <summary>
        /// Fetches all comments for the given campaign ID that expresses approval.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains comments expressing approval.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchApprovalsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                return await campaignCommentRepository.FetchApprovalsAsync(campaignId);
            }
        }

        /// <summary>
        /// Fetches all comments for the given campaign ID that expresses opposal.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains comments expressing opposal.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchOpposalsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignCommentRepository = scope.ServiceProvider.GetRequiredService<CampaignCommentRepository>();
                return await campaignCommentRepository.FetchOpposalsAsync(campaignId);

            }
        }

        public async Task<Campaign> FetchCampaignAsync(Snowflake userId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                return await campaignRepository.FetchCampaignByUserIdAsync(userId);
            }
        }
        /// <summary>
        /// Fetches a list of all ongoing campaigns.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Campaign}"/> that contains all ongoing campaigns.</returns>
        public async Task<IEnumerable<Campaign>> FetchOngoingCampaignsAsync()
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            using (var scope = ServiceProvider.CreateScope())
            {
                var campaignRepository = scope.ServiceProvider.GetRequiredService<CampaignRepository>();
                return await campaignRepository.FetchAllAsync();

            }
        }
    }
}