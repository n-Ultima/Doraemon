using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models.Promotion;
using Doraemon.Services.PromotionServices;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Promotions")]
    [Group("promotion", "promotions")]
    [Description("Provides utilities involving promoting users to the Associate role.")]
    public class PromotionModule : DiscordGuildModuleBase
    {
        private readonly PromotionService _promotionService;
        public PromotionModule(PromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [Command("nominate")]
        [Description("Nominate a user to be promoted to Associate.")]
        public async Task NominateUserAsync(
            [Description("The user to be nominated.")] 
                IMember user,
            [Description("The reason/starting arguments for the nomination.")] [Remainder]
                string reason)
        {
            await _promotionService.NominateUserAsync(user.Id, Context.Author.Id, reason, Context.Guild.Id,
                Context.Channel.Id);
            await Context.AddConfirmationAsync();
        }

        [Command("", "list")]
        [Description("List all current promotions.")]
        public async Task ListAllPromotionsAsync()
        {
            var builder = new StringBuilder();
            foreach (var campaign in await _promotionService.FetchOngoingCampaignsAsync())
            {
                builder.AppendLine($"**Campaign ID:** `{campaign.Id}`");
                builder.AppendLine($"**Campaign Nominee:** <@{campaign.UserId}>");
                builder.AppendLine($"**Campaign Initiator:** <@{campaign.InitiatorId}>");
                builder.AppendLine($"**Reason For Campaign:** {campaign.ReasonForCampaign}");
                builder.AppendLine("---");
            }

            var embed = new LocalEmbed()
                .WithTitle("Current Campaigns")
                .WithDescription(builder.ToString())
                .WithColor(DColor.DarkPurple);
            await Context.Channel.SendMessageAsync(new LocalMessage()
                .WithEmbeds(embed));
        }

        [Command("approve")]
        [Description("Approve of a campaign.")]
        public async Task ApproveCampainAsync(
            [Description("The ID of the campaign to approve.")]
                string campaignId)
        {
            await _promotionService.ApproveCampaignAsync(Context.Author.Id, campaignId);
            await Context.AddConfirmationAsync();
        }

        [Command("comment")]
        [Description("Comments on an ongoing campaign.")]
        public async Task CommentOnCampaignAsync(
            [Description("The ID of the campaign.")]
                string campaignId, [Remainder] 
            [Description("The content of the comment.")]
                string comment)
        {
            await _promotionService.AddNoteToCampaignAsync(Context.Author.Id, campaignId, comment);
            await Context.AddConfirmationAsync();
        }

        [Command("info")]
        [Description("Fetch info related to a specific campaign.")]
        public async Task FetchCampaignInfoAsync(
            [Description("The ID of the campaign.")] 
                string campaignId)
        {

            var otherComments = await _promotionService.FetchCustomCommentsForCampaignAsync(campaignId);
            var approvals = await _promotionService.FetchApprovalsForCampaignAsync(campaignId);
            var opposals = await _promotionService.FetchOpposalsForCampaignAsync(campaignId);
            var builder = new StringBuilder();
            foreach (var comment in otherComments)
            {
                builder.AppendLine($"<@{comment.AuthorId}> ~ {comment.Content}");
                builder.AppendLine();
            }

            var embed = new LocalEmbed()
                .WithTitle("Current Campaign Approvals/Opposals")
                .WithColor(DColor.DarkPurple)
                .AddField("Approvals", approvals.Count(), true)
                .AddField("Opposals", opposals.Count(), true);
            await Context.Channel.SendMessageAsync(new LocalMessage()
                .WithEmbeds(embed));
            var embed2 = new LocalEmbed()
                .WithTitle("Comments")
                .WithColor(DColor.DarkPurple)
                .WithDescription(builder.ToString());
            await Context.Channel.SendMessageAsync(new LocalMessage()
                .WithEmbeds(embed2));
        }

        [Command("oppose")]
        [Description("Oppose an existing campaign.")]
        public async Task OpposeCampaignAsync(
            [Description("Express opposal for an ongoing campaign.")]
                string campaignId)
        {
            await _promotionService.OpposeCampaignAsync(Context.Author.Id, campaignId);
            await Context.AddConfirmationAsync();
        }

        [Command("accept")]
        [Description("Accept an ongoing campaign, and promote the user.")]
        public async Task AcceptCampaignAsync(
            [Description("The ID of the campaign to accept.")]
                string campaignId)
        {
            await _promotionService.AcceptCampaignAsync(campaignId,  Context.Guild.Id);
            await Context.AddConfirmationAsync();
        }

        [Command("reject")]
        [Description("Reject an ongoing campaign.")]
        public async Task RejectCampaignAsync(
            [Description("The ID of the campaign to reject.")]
                string campaignId)
        {
            await _promotionService.RejectCampaignAsync(campaignId, Context.Guild.Id);
            await Context.AddConfirmationAsync();
        }
    }
}