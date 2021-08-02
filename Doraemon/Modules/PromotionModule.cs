//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;
//using Doraemon.Common.Extensions;
//using Doraemon.Data;
//using Doraemon.Data.Models.Promotion;
//using Doraemon.Services.PromotionServices;
//using Microsoft.EntityFrameworkCore;
//
//namespace Doraemon.Modules
//{
//    [Name("Promotions")]
//    [Group("promotion")]
//    [Alias("promotions")]
//    [Summary("Provides utilities involving promoting users to the Associate role.")]
//    public class PromotionModule : ModuleBase<SocketCommandContext>
//    {
//        private const string ApprovalMessage = "I approve of this campaign.";
//        private const string OpposalMessage = "I do not approve of this campaign.";
//        private readonly PromotionService _promotionService;
//
//        public PromotionModule(PromotionService promotionService)
//        {
//            _promotionService = promotionService;
//        }
//
//        [Command("nominate")]
//        [Summary("Nominate a user to be promoted to Associate.")]
//        public async Task NominateUserAsync(
//            [Summary("The user to be nominated.")] 
//                IGuildUser user,
//            [Summary("The reason/starting arguments for the nomination.")] [Remainder]
//                string reason)
//        {
//            await _promotionService.NominateUserAsync(user.Id, Context.User.Id, reason, Context.Guild.Id,
//                Context.Channel.Id);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command("list")]
//        [Alias("")]
//        [Summary("List all current promotions.")]
//        public async Task ListAllPromotionsAsync()
//        {
//            var builder = new StringBuilder();
//            foreach (var campaign in await _promotionService.FetchOngoingCampaignsAsync(Context.User.Id))
//            {
//                builder.AppendLine($"**Campaign ID:** `{campaign.Id}`");
//                builder.AppendLine($"**Campaign Nominee:** <@{campaign.UserId}>");
//                builder.AppendLine($"**Campaign Initiator:** <@{campaign.InitiatorId}>");
//                builder.AppendLine($"**Reason For Campaign:** {campaign.ReasonForCampaign}");
//                builder.AppendLine("---");
//            }
//
//            var embed = new EmbedBuilder()
//                .WithTitle("Current Campaigns")
//                .WithDescription(builder.ToString())
//                .WithColor(Color.DarkPurple)
//                .Build();
//            await ReplyAsync(embed: embed);
//        }
//
//        [Command("approve")]
//        [Summary("Approve of a campaign.")]
//        public async Task ApproveCampainAsync(
//            [Summary("The ID of the campaign to approve.")]
//                string campaignId)
//        {
//            await _promotionService.ApproveCampaignAsync(Context.User.Id, campaignId);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command("comment")]
//        [Summary("Comments on an ongoing campaign.")]
//        public async Task CommentOnCampaignAsync(
//            [Summary("The ID of the campaign.")]
//                string campaignId, [Remainder] 
//            [Summary("The content of the comment.")]
//                string comment)
//        {
//            await _promotionService.AddNoteToCampaignAsync(Context.User.Id, campaignId, comment);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command("info")]
//        [Summary("Fetch info related to a specific campaign.")]
//        public async Task FetchCampaignInfoAsync(
//            [Summary("The ID of the campaign.")] 
//                string campaignId)
//        {
//
//            var otherComments = await _promotionService.FetchCustomCommentsForCampaignAsync(campaignId, Context.User.Id);
//            var approvals = await _promotionService.FetchApprovalsForCampaignAsync(campaignId, Context.User.Id);
//            var opposals = await _promotionService.FetchOpposalsForCampaignAsync(campaignId, Context.User.Id);
//            var builder = new StringBuilder();
//            foreach (var comment in otherComments)
//            {
//                builder.AppendLine($"<@{comment.AuthorId}> ~ {comment.Content}");
//                builder.AppendLine();
//            }
//
//            var embed = new EmbedBuilder()
//                .WithTitle("Current Campaign Approvals/Opposals")
//                .WithColor(Color.DarkPurple)
//                .AddField("Approvals", approvals.Count(), true)
//                .AddField("Opposals", opposals.Count(), true);
//            await ReplyAsync(embed: embed.Build());
//            var embed2 = new EmbedBuilder()
//                .WithTitle("Comments")
//                .WithColor(Color.DarkPurple)
//                .WithDescription(builder.ToString());
//            await ReplyAsync(embed: embed2.Build());
//        }
//
//        [Command("oppose")]
//        [Summary("Oppose an existing campaign.")]
//        public async Task OpposeCampaignAsync(
//            [Summary("Express opposal for an ongoing campaign.")]
//                string campaignId)
//        {
//            await _promotionService.OpposeCampaignAsync(Context.User.Id, campaignId);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command("accept")]
//        [Summary("Accept an ongoing campaign, and promote the user.")]
//        public async Task AcceptCampaignAsync(
//            [Summary("The ID of the campaign to accept.")]
//                string campaignId)
//        {
//            await _promotionService.AcceptCampaignAsync(campaignId, Context.User.Id, Context.Guild.Id);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command("reject")]
//        [Summary("Reject an ongoing campaign.")]
//        public async Task RejectCampaignAsync(
//            [Summary("The ID of the campaign to reject.")]
//                string campaignId)
//        {
//            await _promotionService.RejectCampaignAsync(campaignId, Context.User.Id, Context.Guild.Id);
//            await Context.AddConfirmationAsync();
//        }
//    }
//}