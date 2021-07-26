using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Services.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Modules
{
    [Name("Infractions")]
    [Group("infraction")]
    [Alias("infractions")]
    [Summary("Provides utilities for searching and managing infractions.")]
    public class InfractionModule : ModuleBase<SocketCommandContext>
    {
        private readonly InfractionService _infractionService;

        private readonly DoraemonContext _doraemonContext;

        public InfractionModule(InfractionService infractionService, DoraemonContext doraemonContext)
        {
            _infractionService = infractionService;
            _doraemonContext = doraemonContext;
        }

        [Command]
        [Alias("search")]
        [Summary("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Summary("The user whose infractions to be displayed.")]
                SocketGuildUser user)
        {
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var builder = new EmbedBuilder()
                .WithTitle($"Infractions for {user.GetFullUsername()}")
                .WithDescription($"Has **{infractions.Count()}** current infractions.")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = await Context.Client.Rest.GetUserAsync(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField(
                    $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} by {moderator.GetFullUsername()}",
                    $"Reason: {infraction.Reason}");
            }

            var embed = builder.Build();
            await ReplyAsync(embed: embed);
        }

        [Command]
        [Alias("search")]
        [Summary("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Summary("The ID of the user to search for.")]
                ulong id)
        {
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var user = await Context.Client.Rest.GetUserAsync(id);
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var builder = new EmbedBuilder()
                .WithTitle($"Infractions for {user.GetFullUsername()}")
                .WithDescription($"Has **{infractions.Count()}** current infractions.")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = await Context.Client.Rest.GetUserAsync(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField(
                    $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.GetFullUsername()}",
                    $"Reason: {infraction.Reason}");
            }

            
            var embed = builder.Build();
            await ReplyAsync(embed: embed);
        }

        [Command("delete")]
        [Alias("remove")]
        [Summary("Deletes an infraction, causing it to no longer show up in future queries.")]
        public async Task DeleteInfractionAsync(
            [Summary("The ID of the infraction")] string infractionId,
            [Summary("The reason for removing the infraction.")] [Remainder]
            string reason = null)
        {
            await _infractionService.RemoveInfractionAsync(infractionId, reason ?? "Not specified", Context.User.Id);
            await Context.AddConfirmationAsync();
        }

        [Command("update")]
        [Summary("Updates a current infraction with the given reason.")]
        public async Task UpdateInfractionAsync(
            [Summary("The ID of the infraction to update.")]
            string infractionId,
            [Summary("The new reason for the infraction")] [Remainder]
            string reason)
        {
            await _infractionService.UpdateInfractionAsync(infractionId, reason);
            await Context.AddConfirmationAsync();
        }

        private static string GetEmojiForInfractionType(InfractionType infractionType)
        {
            return infractionType switch
            {
                InfractionType.Note => "📝",
                InfractionType.Warn => "⚠️",
                InfractionType.Mute => "🔇",
                InfractionType.Ban => "🔨",
                _ => "❔"
            };
        }
    }
}