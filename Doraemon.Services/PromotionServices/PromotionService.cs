﻿using System;
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
using Microsoft.SqlServer.Server;

namespace Doraemon.Services.PromotionServices
{
    [DoraemonService]
    public class PromotionService : DiscordBotService
    {
        private const string DefaultApprovalMessage = "I approve of this campaign.";
        private const string DefaultOpposalMessage = "I do not approve of this campaign.";
        private readonly CampaignCommentRepository _campaignCommentRepository;
        private readonly CampaignRepository _campaignRepository;
        private readonly AuthorizationService _authorizationService;

        public PromotionService(CampaignCommentRepository campaignCommentRepository,
            AuthorizationService authorizationService,
            CampaignRepository campaignRepository)
        {
            _authorizationService = authorizationService;
            _campaignRepository = campaignRepository;
            _campaignCommentRepository = campaignCommentRepository;
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


            var embed = new LocalEmbed()
                .WithTitle("Campaign Started")
                .WithDescription(
                    $"A campaign was started for <@{userId}>, with reason: `{comment}`\nPlease save this ID, it will be needed for anything involving this campaign: `{ID}`")
                .WithColor(DColor.Purple);
            var guild = Bot.GetGuild(guildId);
            var channel = guild.GetChannel(channelId) as ITextChannel;
            await channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
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
        public async Task ApproveCampaignAsync(Snowflake authorId, string campaignId)
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
        public async Task OpposeCampaignAsync(Snowflake authorId, string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionComment);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
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
        /// <param name="guildId">The ID of the guild that the campaign originated from.</param>
        /// <returns></returns>
        public async Task RejectCampaignAsync(string campaignId, Snowflake guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionManage);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            var promoComments = await _campaignCommentRepository.FetchAllAsync(campaignId);
            if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
            await _campaignRepository.DeleteAsync(promo);
            await _campaignCommentRepository.DeleteAllAsync(promoComments);
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
            var guild = Bot.GetGuild(guildId);
            var role = Bot.GetRole(guild.Id, DoraemonConfig.PromotionRoleId);
            var promo = await _campaignRepository.FetchAsync(campaignId);
            var promoComments = await _campaignCommentRepository.FetchAllAsync(campaignId);
            var user = guild.GetMember(promo.UserId);
            if (user is null)
            {
                await _campaignRepository.DeleteAsync(promo);
                await _campaignCommentRepository.DeleteAllAsync(promoComments);
                throw new ArgumentException(
                    "The user involved in this campaign has left the server, thus, the campaign is automatically rejected.");
            }

            await user.GrantRoleAsync(role.Id);
            if (promo is null) throw new ArgumentException("The campaign ID provided is not valid.");
            await _campaignRepository.DeleteAsync(promo);
            await _campaignCommentRepository.DeleteAllAsync(promoComments);
            var promotionLog = guild.GetChannel(DoraemonConfig.LogConfiguration.PromotionLogChannelId) as ITextChannel;
            var promoLogEmbed = new LocalEmbed()
                .WithAuthor(user)
                .WithTitle("The campaign is over!")
                .WithDescription(
                    $"Staff accepted the campaign, and **{user.Tag}** was promoted to <@&{DoraemonConfig.PromotionRoleId}>!🎉")
                .WithFooter("Congrats on the promotion!");
            await promotionLog.SendMessageAsync(new LocalMessage().WithEmbeds(promoLogEmbed));
        }

        /// <summary>
        /// Fetches a list of custom comments(comments that aren't matching the <see cref="DefaultApprovalMessage"/> or the <see cref="DefaultOpposalMessage"/>.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/></returns>
        public async Task<IEnumerable<CampaignComment>> FetchCustomCommentsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchCustomCommentsAsync(campaignId);
        }

        /// <summary>
        /// Fetches all comments for the given campaign ID that expresses approval.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains comments expressing approval.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchApprovalsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchApprovalsAsync(campaignId);
        }

        /// <summary>
        /// Fetches all comments for the given campaign ID that expresses opposal.
        /// </summary>
        /// <param name="campaignId">The ID value of the campaign.</param>
        /// <returns>A <see cref="IEnumerable{CampaignComment}"/> that contains comments expressing opposal.</returns>
        public async Task<IEnumerable<CampaignComment>> FetchOpposalsForCampaignAsync(string campaignId)
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignCommentRepository.FetchOpposalsAsync(campaignId);
        }

        /// <summary>
        /// Fetches a list of all ongoing campaigns.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Campaign}"/> that contains all ongoing campaigns.</returns>
        public async Task<IEnumerable<Campaign>> FetchOngoingCampaignsAsync()
        {
            _authorizationService.RequireClaims(ClaimMapType.PromotionRead);
            return await _campaignRepository.FetchAllAsync();
        }
    }
}