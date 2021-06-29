using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Services.Moderation;
using Doraemon.Data.Models;
using Doraemon.Common.Attributes;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Data;
using Doraemon.Common.Extensions;

namespace Doraemon.Modules
{
    [Name("Infractions")]
    [Group("infraction")]
    [Alias("infractions")]
    [Summary("Provides utilities for searching and managing infractions.")]
    public class InfractionModule : ModuleBase<SocketCommandContext>
    {
        public InfractionService _infractionService;
        public InfractionModule(InfractionService infractionService)
        {
            _infractionService = infractionService;
        }
        [Command]
        [Alias("search")]
        [Summary("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Summary("The user whose infractions to be displayed.")]
                SocketGuildUser user)
        {
            if ((Context.Channel as IGuildChannel).IsPublic())
            {
                return;
            }
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id, Context.User.Id);
            var builder = new EmbedBuilder()
                .WithTitle($"Infractions for {user.GetFullUsername()}")
                .WithDescription($"Has **{infractions.Count()}** current infractions.")
                .WithColor(new Color(0xA3BF0B));
            foreach(var infraction in infractions)
            {
                var moderator = await Context.Client.Rest.GetUserAsync(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField($"{infraction.Id} - \\{emoji} {infraction.Type} - Moderator: {moderator.GetFullUsername()}", $"Reason: {infraction.Reason}");
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
            if ((Context.Channel as IGuildChannel).IsPublic())
            {
                return;
            }
            var user = await Context.Client.Rest.GetUserAsync(id);
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id, Context.User.Id);
            var builder = new EmbedBuilder()
                .WithTitle($"Infractions for {user.GetFullUsername()}")
                .WithDescription($"Has **{infractions.Count()}** current infractions.")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = await Context.Client.Rest.GetUserAsync(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField($"{infraction.Id} - \\{emoji} {infraction.Type} - Moderator: {moderator.GetFullUsername()}", $"Reason: {infraction.Reason}");
            }
            var embed = builder.Build();
            await ReplyAsync(embed: embed);

        }
        [Command("delete")]
        [Alias("remove")]
        [Summary("Deletes an infraction, causing it to no longer show up in future queries.")]
        public async Task DeleteInfractionAsync(
            [Summary("The ID of the infraction")]
                string infractionId,
            [Summary("The reason for removing the infraction.")]
                [Remainder] string reason = null)
        {
            await _infractionService.RemoveInfractionAsync(infractionId, reason ?? "Not specified", Context.User.Id, true);
            await Context.AddConfirmationAsync();
        }
        [Command("update")]
        [Summary("Updates a current infraction with the given reason.")]
        public async Task UpdateInfractionAsync(
            [Summary("The ID of the infraction to update.")]
                string infractionId,
            [Summary("The new reason for the infraction")]
                [Remainder] string reason)
        {
            await _infractionService.UpdateInfractionAsync(infractionId, Context.User.Id, reason);
            await Context.AddConfirmationAsync();
        }
        private static string GetEmojiForInfractionType(InfractionType infractionType)
            => infractionType switch
            {
                InfractionType.Note => "📝",
                InfractionType.Warn => "⚠️",
                InfractionType.Mute => "🔇",
                InfractionType.Ban => "🔨",
                _ => "❔",
            };
    }
}
