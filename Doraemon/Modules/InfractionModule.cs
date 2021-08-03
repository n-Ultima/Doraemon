using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Services.Moderation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Infractions")]
    [Group("infraction", "infractions")]
    [Description("Provides utilities for searching and managing infractions.")]
    public class InfractionModule : DiscordGuildModuleBase
    {
        private readonly InfractionService _infractionService;

        private readonly DoraemonContext _doraemonContext;

        public InfractionModule(InfractionService infractionService, DoraemonContext doraemonContext)
        {
            _infractionService = infractionService;
            _doraemonContext = doraemonContext;
        }

        [Command("", "search")]
        [Description("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Description("The user whose infractions to be displayed.")]
                IMember user)
        {
            if ((Context.Channel as IGuildChannel).IsPublic()) 
                return;
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var warns = infractions.Where(x => x.Type == InfractionType.Warn).ToList();
            var mutes = infractions.Where(x => x.Type == InfractionType.Mute).ToList();
            var notes = infractions.Where(x => x.Type == InfractionType.Note).ToList();
            var bans = infractions.Where(x => x.Type == InfractionType.Ban).ToList();
            var builder = new LocalEmbed()
                .WithTitle($"Infractions for {user.Tag}")
                .WithDescription($"This member has {notes.Count} notes, {warns.Count} warnings, {mutes.Count} mutes, and {bans.Count} bans")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(user.Id);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField(
                    $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}",
                    $"Reason: {infraction.Reason}");
            }

            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(builder));
        }

        [Command("", "search")]
        [Description("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Description("The ID of the user to search for.")]
                Snowflake id)
        {
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var user = await Context.Bot.FetchUserAsync(id);
            if (user == null)
                throw new Exception($"The userId provided does not exist.");
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var warns = infractions.Where(x => x.Type == InfractionType.Warn).ToList();
            var mutes = infractions.Where(x => x.Type == InfractionType.Mute).ToList();
            var notes = infractions.Where(x => x.Type == InfractionType.Note).ToList();
            var bans = infractions.Where(x => x.Type == InfractionType.Ban).ToList();
            var builder = new LocalEmbed()
                .WithTitle($"Infractions for {user.Tag}")
                .WithDescription($"This member has {notes.Count} notes, {warns.Count} warnings, {mutes.Count} mutes, and {bans.Count} bans")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                builder.AddField(
                    $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                    $"Reason: {infraction.Reason}");
            }

            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(builder));
        }

        [Command("delete", "remove")]
        [Description("Deletes an infraction, causing it to no longer show up in future queries.")]
        public async Task DeleteInfractionAsync(
            [Description("The ID of the infraction")]
                string infractionId,
            [Description("The reason for removing the infraction.")] [Remainder]
                string reason)
        {
            await _infractionService.RemoveInfractionAsync(infractionId, reason ?? "Not specified", Context.Author.Id);
            await Context.AddConfirmationAsync();
        }

        [Command("update")]
        [Description("Updates a current infraction with the given reason.")]
        public async Task UpdateInfractionAsync(
            [Description("The ID of the infraction to update.")]
                string infractionId,
            [Description("The new reason for the infraction")] [Remainder]
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