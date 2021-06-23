using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Data;
using Doraemon.Common.Extensions;
using Doraemon.Data.Services;
using Doraemon.Common.Attributes;
using Doraemon.Data.Models.Promotion;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Modules
{
    [Name("Promotions")]
    [Group("promotion")]
    [Alias("promotions")]
    [Summary("Provides utilities involving promoting users to the Associate role.")]
    public class PromotionModule : ModuleBase<SocketCommandContext>
    {
        public DoraemonContext _doraemonContext;
        public PromotionService _promotionService;
        public const string ApprovalMessage = "I approve of this campaign.";
        public const string OpposalMessage = "I do not approve of this campaign.";
        public PromotionModule(DoraemonContext doraemonContext, PromotionService promotionService)
        {
            _doraemonContext = doraemonContext;
            _promotionService = promotionService;
        }
        [Command("nominate")]
        [Summary("Nominate a user to be promoted to Associate.")]
        public async Task NominateUserAsync(
            [Summary("The user to be nominated.")]
                IGuildUser user,
            [Summary("The reason/starting arguments for the nomination.")]
                [Remainder] string reason)
        {
            await _promotionService.NominateUserAsync(user.Id, Context.User.Id, reason, Context.Guild.Id, Context.Channel.Id);
            await Context.AddConfirmationAsync();
        }
        [Command("list")]
        [Alias("")]
        [Summary("List all current promotions.")]
        public async Task ListAllPromotionsAsync()
        {
            var builder = new StringBuilder();
            foreach (var campaign in _doraemonContext.Campaigns)
            {
                builder.AppendLine($"**Campaign ID:** `{campaign.Id}`");
                builder.AppendLine($"**Campaign Nominee:** <@{campaign.UserId}>");
                builder.AppendLine($"**Campaign Initiator:** <@{campaign.InitiatorId}>");
                builder.AppendLine($"**Reason For Campaign:** {campaign.ReasonForCampaign}");
                builder.AppendLine($"---");
            }
            var embed = new EmbedBuilder()
                .WithTitle("Current Campaigns")
                .WithDescription(builder.ToString())
                .WithColor(Color.DarkPurple)
                .Build();
            await ReplyAsync(embed: embed);
        }
        [Command("approve")]
        public async Task ApproveCampainAsync(string campaignId)
        {
            await _promotionService.ApproveCampaignAsync(Context.User.Id, campaignId);
            await Context.AddConfirmationAsync();
        }
        [Command("comment")]
        public async Task CommentOnCampaignAsync(string campaignId, [Remainder] string comment)
        {
            await _promotionService.AddNoteToCampaignAsync(Context.User.Id, campaignId, comment);
            await Context.AddConfirmationAsync();
        }
        [Command("info")]
        [Summary("Fetch info related to a specific campaign.")]
        public async Task FetchCampaignInfoAsync(
            [Summary("The ID of the campaign.")]
                string campaignId)
        {
            var comments = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.Id == campaignId)
                .ToListAsync();
            var approvals = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == ApprovalMessage)
                .ToListAsync();
            var opposals = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content == OpposalMessage)
                .ToListAsync();
            var otherComments = await _doraemonContext
                .Set<CampaignComment>()
                .AsQueryable()
                .Where(x => x.CampaignId == campaignId)
                .Where(x => x.Content != OpposalMessage)
                .Where(x => x.Content != ApprovalMessage)
                .ToListAsync();
            var builder = new StringBuilder();
            foreach (var comment in otherComments)
            {
                builder.AppendLine($"<@{comment.AuthorId}> ~ {comment.Content}");
                builder.AppendLine();
            }
            var embed = new EmbedBuilder()
                .WithTitle("Current Campaign Approvals/Opposals")
                .WithColor(Color.DarkPurple)
                .AddField("Approvals", approvals.Count(), true)
                .AddField("Opposals", opposals.Count(), true);
            await ReplyAsync(embed: embed.Build());
            var embed2 = new EmbedBuilder()
                .WithTitle("Comments")
                .WithColor(Color.DarkPurple)
                .WithDescription(builder.ToString());
            await ReplyAsync(embed: embed2.Build());
        }
        [Command("oppose")]
        [Summary("Oppose an existing campaign.")]
        public async Task OpposeCampaignAsync(string campaignId)
        {
            await _promotionService.OpposeCampaignAsync(Context.User.Id, campaignId);
            await Context.AddConfirmationAsync();
        }
        [Command("accept")]
        [Summary("Accept an ongoing campaign, and promote the user.")]
        public async Task AcceptCampaignAsync(
            [Summary("The ID of the campaign to accept.")]
                string campaignId)
        {
            await _promotionService.AcceptCampaignAsync(campaignId, Context.User.Id, Context.Guild.Id);
            await Context.AddConfirmationAsync();
        }
        [Command("reject")]
        [Summary("Reject an ongoing campaign.")]
        public async Task RejectCampaignAsync(
            [Summary("The ID of the campaign to reject.")]
                string campaignId)
        {
            await _promotionService.RejectCampaignAsync(campaignId, Context.User.Id, Context.Guild.Id);
            await Context.AddConfirmationAsync();
        }
    }
}
