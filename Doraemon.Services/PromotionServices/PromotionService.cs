using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Promotion;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;

namespace Doraemon.Services.PromotionServices
{
    [DoraemonService]
    public class PromotionService
    {
        public const string DefaultApprovalMessage = "I approve of this campaign.";
        public const string DefaultOpposalMessage = "I do not approve of this campaign.";
        private readonly CampaignCommentRepository _campaignCommentRepository;
        private readonly CampaignRepository _campaignRepository;
        private readonly AuthorizationService _authorizationService;
        private readonly DiscordSocketClient _client;

        public PromotionService(CampaignCommentRepository campaignCommentRepository,
            AuthorizationService authorizationService, DiscordSocketClient client,
            CampaignRepository campaignRepository)
        {
            _authorizationService = authorizationService;
            _client = client;
            _campaignRepository = campaignRepository;
            _campaignCommentRepository = campaignCommentRepository;
        }

        public static DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Nominates a user to be promoted to the specified role in the <see cref="DoraemonConfiguration"/.>
        /// </summary>
        /// <param name="userId">The ID value of the user being nominated.</param>
        /// <param name="initiatorId">The ID value of the user initiating the campaign.</param>
        /// <param name="comment">The starting comment that the Initiator would like to leave regarding the campaign.</param>
        /// <param name="guildId">The guild that the campaign is happening in.</param>
        /// <param name="channelId">The channel ID that the campaign is launched in.</param>
        /// <returns></returns>
        public async Task NominateUserAsync(ulong userId, ulong initiatorId, string comment, ulong guildId,
            ulong channelId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionStart);
            var promo = await _campaignRepository.FetchCampaignByUserIdAsync(userId);
            if (promo is not null)
                throw new Exception("There is already an ongoing campaign for this user.");
            var ID = DatabaseUtilities.ProduceId();
            await _campaignRepository.CreateAsync(new CampaignCreationData
            {
                Id = ID,
                InitiatorId = initiatorId,
                UserId = userId,
                ReasonForCampaign = comment
            });
            var embed = new EmbedBuilder()
                .WithTitle("Campaign Started")
                .WithDescription(
                    $"A campaign was started for <@{userId}>, with reason: `{comment}`\nPlease save this ID, it will be needed for anything involving this campaign: `{ID}`")
                .WithColor(Color.DarkPurple)
                .Build();
            var guild = _client.GetGuild(guildId);
            var channel = guild.GetTextChannel(channelId);
            await channel.SendMessageAsync(embed: embed);
        }

        /// <summary>
        ///     Adds a custom note to an ongoing campaign.
        /// </summary>
        /// <param name="authorId">The ID value of the author adding the note.</param>
        /// <param name="campaignId">The ID value of the campaign to add this note to.</param>
        /// <param name="note">The content of the note itself.</param>
        /// <returns></returns>
        public async Task AddNoteToCampaignAsync(ulong authorId, string campaignId, string note)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
            var currentPromoNotes = await _campaignCommentRepository.FetchCommentsByContentAsync(campaignId, note);
            if (currentPromoNotes)
                throw new Exception(
                    "There is already an existing comment on the campaign provided that matches the Content provided.");
            await _campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
            {
                AuthorId = authorId,
                CampaignId = campaignId,
                Content = note
            });
        }

        /// <summary>
        ///     Adds a note to the campaign that expresses approval.
        /// </summary>
        /// <param name="authorId">The ID value of the author.</param>
        /// <param name="campaignId">The campaign ID that the note will be applied to.</param>
        /// <returns></returns>
        public async Task ApproveCampaignAsync(ulong authorId, string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            var alreadyVoted = await _campaignCommentRepository.HasUserAlreadyVoted(authorId, campaignId);
            if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
            if (alreadyVoted)
                throw new Exception(
                    "You have already voted for the current campaign, so you cannot vote again.");
            await _campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
            {
                AuthorId = authorId,
                CampaignId = campaignId,
                Content = DefaultApprovalMessage
            });
        }

        /// <summary>
        ///     Adds a note to a campaign that expresses opposal.
        /// </summary>
        /// <param name="authorId">The ID value of the author.</param>
        /// <param name="campaignId">The campaign ID to apply the note to.</param>
        /// <returns></returns>
        public async Task OpposeCampaignAsync(ulong authorId, string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            if (promo is null) throw new ArgumentNullException("The campaign ID provided is not valid.");
            var alreadyVoted = await _campaignCommentRepository.HasUserAlreadyVoted(authorId, campaignId);
            if (alreadyVoted)
                throw new Exception(
                    "You have already voted for the current campaign, so you cannot vote again.");
            await _campaignCommentRepository.CreateAsync(new CampaignCommentCreationData
            {
                AuthorId = authorId,
                CampaignId = campaignId,
                Content = DefaultOpposalMessage
            });
        }

        /// <summary>
        ///     Rejects a campaign, denying it.
        /// </summary>
        /// <param name="campaignId">The ID of the campaign to reject.</param>
        /// <param name="managerId">The user ID attempting to reject the campaign.</param>
        /// <param name="guildId">The ID of the guild that the campaign originated from.</param>
        /// <returns></returns>
        public async Task RejectCampaignAsync(string campaignId, ulong managerId, ulong guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionManage);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            var promoComments = await _campaignCommentRepository.FetchAllAsync(campaignId);
            if (promo is null) throw new ArgumentNullException("The campaign ID provided is not valid.");
            await _campaignRepository.DeleteAsync(promo);
            await _campaignCommentRepository.DeleteAllAsync(promoComments);
        }

        /// <summary>
        ///     Accepts a campaign, promoting the user.
        /// </summary>
        /// <param name="campaignId">The ID of the campaign.</param>
        /// <param name="managerId">The user ID attempting to approve the campaign.</param>
        /// <param name="guildId">The guild ID that the campaign originated from.</param>
        /// <returns></returns>
        public async Task AcceptCampaignAsync(string campaignId, ulong managerId, ulong guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionManage);
            var guild = _client.GetGuild(guildId);
            var role = guild.GetRole(DoraemonConfig.PromotionRoleId);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            var promoComments = await _campaignCommentRepository.FetchAllAsync(campaignId);
            var user = guild.GetUser(promo.UserId);
            if (user is null)
            {
                await _campaignRepository.DeleteAsync(promo);
                await _campaignCommentRepository.DeleteAllAsync(promoComments);
                throw new ArgumentException(
                    "The user involed in this campaign has left the server, thus, the campaign is automatically rejected.");
            }

            await user.AddRoleAsync(role);
            if (promo is null) throw new ArgumentNullException("The campaign ID provided is not valid.");
            await _campaignRepository.DeleteAsync(promo);
            await _campaignCommentRepository.DeleteAllAsync(promoComments);
            var promotionLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.PromotionLogChannelId);
            var promoLogEmbed = new EmbedBuilder()
                .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
                .WithTitle("The campaign is over!")
                .WithDescription(
                    $"Staff accepted the campaign, and {Format.Bold(user.GetFullUsername())} was promoted to <@&{DoraemonConfig.PromotionRoleId}>!🎉")
                .WithFooter("Congrats on the promotion!")
                .Build();
            await promotionLog.SendMessageAsync(embed: promoLogEmbed);
        }

        public async Task<IEnumerable<CampaignComment>> FetchCustomCommentsForCampaignAsync(string campaignId, ulong requestorId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchCustomCommentsAsync(campaignId);
        }
        public async Task<IEnumerable<CampaignComment>> FetchApprovalsForCampaignAsync(string campaignId, ulong requestorId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchApprovalsAsync(campaignId);
        }

        public async Task<IEnumerable<CampaignComment>> FetchOpposalsForCampaignAsync(string campaignId, ulong requestorId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchOpposalsAsync(campaignId);
        }

        public async Task<IEnumerable<Campaign>> FetchOngoingCampaignsAsync(ulong requestorId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignRepository.FetchAllAsync();
        }
    }
}