using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Doraemon.Common;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Doraemon.Common.Utilities;
using Discord;
using Doraemon.Common.Extensions;

namespace Doraemon.Data.Services
{
    public class PromotionService
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public const string DefaultApprovalMessage = "I approve of this campaign.";
        public const string DefaultOpposalMessage = "I do not approve of this campaign.";
        public static DoraemonConfiguration Configuration { get; private set; } = new();
        public PromotionService(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        /// <summary>
        /// Nominates a user for a new campaign.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="comment"></param>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task NominateUserAsync(ulong userId, ulong initiatorId, string comment, ulong guildId, ulong channelId)
        {
            var promo = await _doraemonContext
                .Set<Campaign>()
                .Where(x => x.UserId == userId)
                .AnyAsync();
            if (promo)
            {
                throw new ArgumentException("There is already an ongoing campaign for this user.");
            }
            var ID = await DatabaseUtilities.ProduceIdAsync();
            _doraemonContext.Campaigns.Add(new Campaign { Id = ID, ReasonForCampaign = comment, UserId = userId, InitiatorId = initiatorId });
            await _doraemonContext.SaveChangesAsync();
            var embed = new EmbedBuilder()
                .WithTitle("Campaign Started")
                .WithDescription($"A campaign was started for <@{userId}>, with reason: `{comment}`\nPlease save this ID, it will be needed for anything involving this campaign: `{ID}`")
                .WithColor(Color.DarkPurple)
                .Build();
            var guild = _client.GetGuild(guildId);
            var channel = guild.GetTextChannel(channelId);
            await channel.SendMessageAsync(embed: embed);
        }
        /// <summary>
        /// Adds a note to the campaign provided.
        /// </summary>
        /// <param name="campaignId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task AddNoteToCampaignAsync(ulong authorId, string campaignId, string note)
        {
            var promo = await _doraemonContext
                .Set<Campaign>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .SingleOrDefaultAsync();
            if (promo is null)
            {
                throw new ArgumentException("The campaign ID provided is not valid.");
            }
            var currentPromoNotes = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.Content == note)
                .AnyAsync();
            if (currentPromoNotes)
            {
                throw new ArgumentException("There is already an existing comment on the campaign provided that matches the Content provided.");
            }
            _doraemonContext.CampaignComments.Add(new CampaignComment { Id = await DatabaseUtilities.ProduceIdAsync(), Content = note, AuthorId = authorId, CampaignId = campaignId });
            await _doraemonContext.SaveChangesAsync();
        }
        /// <summary>
        /// Approve an ongoing campaign.
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="campaignId"></param>
        /// <returns></returns>
        public async Task ApproveUserAsync(ulong authorId, string campaignId)
        {
            var promo = await _doraemonContext
                .Set<Campaign>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .AnyAsync();
            var alreadyVoted = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.AuthorId == authorId)
                .AnyAsync();
            if (!promo)
            {
                throw new ArgumentException("The campaign ID provided is not valid.");
            }
            if (alreadyVoted)
            {
                throw new ArgumentException("You have already voted for the current campaign, so you cannot vote again.");
            }
            _doraemonContext.CampaignComments.Add(new CampaignComment { AuthorId = authorId, Content = DefaultApprovalMessage, Id = await DatabaseUtilities.ProduceIdAsync(), CampaignId = campaignId });
            await _doraemonContext.SaveChangesAsync();
        }
        /// <summary>
        /// Oppose an ongoing campaign.
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="campaignId"></param>
        /// <returns></returns>
        public async Task OpposeCampaignAsync(ulong authorId, string campaignId)
        {
            var promo = await _doraemonContext
                .Set<Campaign>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .AnyAsync();
            if (!promo)
            {
                throw new ArgumentException("The campaign ID provided is not valid.");
            }
            var alreadyVoted = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.AuthorId == authorId)
                .AnyAsync();
            if (alreadyVoted)
            {
                throw new ArgumentException("You have already voted for the current campaign, so you cannot vote again.");
            }
            _doraemonContext.CampaignComments.Add(new CampaignComment { AuthorId = authorId, Content = DefaultOpposalMessage, Id = await DatabaseUtilities.ProduceIdAsync(), CampaignId = campaignId });
            await _doraemonContext.SaveChangesAsync();
        }
        /// <summary>
        /// Reject a campaign and end it.
        /// </summary>
        /// <param name="campaignId"></param>
        /// <returns></returns>
        public async Task RejectCampaignAsync(string campaignId, ulong guildId)
        {
            var promo = await _doraemonContext
                .Set<Campaign>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .SingleOrDefaultAsync();
            var promoComments = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .ToListAsync();
            if (promo is null)
            {
                throw new ArgumentException("The campaign ID provided is not valid.");
            }
            _doraemonContext.Campaigns.Remove(promo);
            await _doraemonContext.SaveChangesAsync();
            foreach (var comment in promoComments)
            {
                _doraemonContext.CampaignComments.Remove(comment);
                await _doraemonContext.SaveChangesAsync();
            }
        }
        /// <summary>
        /// Accepts the campaign provided, promoting the user.
        /// </summary>
        /// <param name="campaignId"></param>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public async Task AcceptCampaignAsync(string campaignId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            var role = guild.GetRole(Configuration.PromotionRoleId);
            var promo = await _doraemonContext
                .Set<Campaign>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .SingleOrDefaultAsync();
            var promoComments = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .ToListAsync();
            var user = guild.GetUser(promo.UserId);
            var n = user.Username + "#" + user.Discriminator;
            await user.AddRoleAsync(role);
            if (promo is null)
            {
                throw new ArgumentException("The campaign ID provided is not valid.");
            }
            _doraemonContext.Campaigns.Remove(promo);
            await _doraemonContext.SaveChangesAsync();
            foreach (var comment in promoComments)
            {
                _doraemonContext.CampaignComments.Remove(comment);
                await _doraemonContext.SaveChangesAsync();
            }
            var promotionLog = guild.GetTextChannel(Configuration.LogConfiguration.PromotionLogChannelId);
            var promoLogEmbed = new EmbedBuilder()
                .WithAuthor(n, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithTitle("The campaign is over!")
                .WithDescription($"Staff accepted the campaign, and **{await user.GetFullUsername()}** was promoted to <@&{Configuration.PromotionRoleId}>!🎉")
                .WithFooter("Congrats on the promotion!")
                .Build();
            await promotionLog.SendMessageAsync(embed: promoLogEmbed);
        }
    }
}
