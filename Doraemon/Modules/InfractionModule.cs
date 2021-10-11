﻿using System;
using System.IO;
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
using Doraemon.Data.Models.Core;
using Doraemon.Data.TypeReaders;
using Doraemon.Services.Moderation;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Infractions")]
    [Group("infraction", "infractions", "case", "cases")]
    [Description("Provides utilities for searching and managing infractions.")]
    public class InfractionModule : DoraemonGuildModuleBase
    {
        private readonly InfractionService _infractionService;


        public InfractionModule(InfractionService infractionService)
        {
            _infractionService = infractionService;
        }

        [Command("", "search")]
        [RequireClaims(ClaimMapType.InfractionView)]
        [Description("Lists all the infractions of a user.")]
        public async Task<DiscordCommandResult> ListUserInfractionsAsync(
            [Description("The user whose infractions to be displayed.")]
            IMember user)
        {
            if ((Context.Channel as IGuildChannel).IsPublic())
                return null;
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var warns = infractions.Where(x => x.Type == InfractionType.Warn).ToList();
            var mutes = infractions.Where(x => x.Type == InfractionType.Mute).ToList();
            var notes = infractions.Where(x => x.Type == InfractionType.Note).ToList();
            var bans = infractions.Where(x => x.Type == InfractionType.Ban).ToList();
            var kicks = infractions.Where(x => x.Type == InfractionType.Kick).ToList();
            var builder = new LocalEmbed()
                .WithTitle($"Infractions for {user.Tag}")
                .WithDescription(
                    $"This member has {notes.Count} {FormatInfractionCounts(InfractionType.Note, notes.Count)}, {warns.Count} {FormatInfractionCounts(InfractionType.Warn, warns.Count)}, {mutes.Count} {FormatInfractionCounts(InfractionType.Mute, mutes.Count)}, {kicks.Count} {FormatInfractionCounts(InfractionType.Kick, kicks.Count)}, and {bans.Count} {FormatInfractionCounts(InfractionType.Ban, bans.Count)}.")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                if (infraction.Duration == null)
                {
                    builder.AddField(
                        $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                        $"Reason: {infraction.Reason}");
                }
                else
                {
                    builder.AddField(
                        $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                        $"Reason: {infraction.Reason}\nDuration: {infraction.Duration.Value.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year, precision: 10)}");
                }
            }

            return Response(builder);
        }

        [Command("", "search")]
        [RequireClaims(ClaimMapType.InfractionView)]
        [Description("Lists all the infractions of a user.")]
        public async Task<DiscordCommandResult> ListUserInfractionsAsync(
            [Description("The ID of the user to search for.")]
            Snowflake id)
        {
            if ((Context.Channel as IGuildChannel).IsPublic())
                return null;
            var user = await Context.Bot.FetchUserAsync(id);
            if (user == null)
                throw new Exception($"The userId provided does not exist.");
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var warns = infractions.Where(x => x.Type == InfractionType.Warn).ToList();
            var mutes = infractions.Where(x => x.Type == InfractionType.Mute).ToList();
            var notes = infractions.Where(x => x.Type == InfractionType.Note).ToList();
            var bans = infractions.Where(x => x.Type == InfractionType.Ban).ToList();
            var kicks = infractions.Where(x => x.Type == InfractionType.Kick).ToList();
            var builder = new LocalEmbed()
                .WithTitle($"Infractions for {user.Tag}")
                .WithDescription(
                    $"This member has {notes.Count} {FormatInfractionCounts(InfractionType.Note, notes.Count)}, {warns.Count} {FormatInfractionCounts(InfractionType.Warn, warns.Count)}, {mutes.Count} {FormatInfractionCounts(InfractionType.Mute, mutes.Count)}, {kicks.Count} {FormatInfractionCounts(InfractionType.Kick, kicks.Count)}, and {bans.Count} {FormatInfractionCounts(InfractionType.Ban, bans.Count)}.")
                .WithColor(new Color(0xA3BF0B));
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(infraction.ModeratorId);
                var emoji = GetEmojiForInfractionType(infraction.Type);
                if (infraction.Duration == null)
                {
                    builder.AddField(
                        $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                        $"Reason: {infraction.Reason}");
                }
                else
                {
                    builder.AddField(
                        $"{infraction.Id} - \\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                        $"Reason: {infraction.Reason}\nDuration: {infraction.Duration.Value.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year, precision: 10)}");
                }
            }

            return Response(builder);
        }

        [Command("", "info")]
        [RequireClaims(ClaimMapType.InfractionView)]
        [Description("Fetches information about the infraction provided.")]
        public async Task<DiscordCommandResult> FetchInfractionAsync(
            [Description("The ID of the infraction to query for.")] [Remainder]
            string infractionId)
        {
            var infraction = await _infractionService.FetchInfractionAsync(infractionId);
            if (infraction == null)
                throw new Exception($"The infraction ID provided does not exist.");
            var emoji = GetEmojiForInfractionType(infraction.Type);
            var moderator = await Bot.FetchUserAsync(infraction.ModeratorId);
            var embed = new LocalEmbed();
            if (infraction.Duration == null)
            {
                embed.AddField($"Subject: {infraction.SubjectId}\\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                    $"Reason: {infraction.Reason}");
            }
            else
            {
                embed.AddField($"Subject: {infraction.SubjectId}\\{emoji} {infraction.Type} - Created On {infraction.CreatedAt.ToString("M")} UTC by {moderator.Tag}",
                    $"Reason: {infraction.Reason}\nDuration: {infraction.Duration.Value.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year, precision: 10)}");
            }

            return Response(embed);
        }

        [Command("delete", "remove")]
        [RequireClaims(ClaimMapType.InfractionDelete)]
        [Description("Deletes an infraction, causing it to no longer show up in future queries.")]
        public async Task<DiscordCommandResult> DeleteInfractionAsync(
            [Description("The ID of the infraction")]
            string infractionId,
            [Description("The reason for removing the infraction.")] [Remainder]
            string reason)
        {
            await _infractionService.RemoveInfractionAsync(infractionId, reason ?? "Not specified", Context.Author.Id);
            return Confirmation();
        }

        [Command("update")]
        [RequireClaims(ClaimMapType.InfractionUpdate)]
        [Description("Updates a current infraction with the given reason.")]
        public async Task<DiscordCommandResult> UpdateInfractionAsync(
            [Description("The ID of the infraction to update.")]
            string infractionId,
            [Description("The new reason for the infraction")] [Remainder]
            string reason)
        {
            var x = new TimeSpanTypeReader();
            if (x.TryParseTimeSpan(reason, out var result))
            {
                return await UpdateInfractionAsync(infractionId, result);
            }

            await _infractionService.UpdateInfractionAsync(infractionId, reason);
            return Confirmation();
        }

        [Command("update")]
        [RequireClaims(ClaimMapType.InfractionUpdate)]
        [Description(
            "Updates the given infraction with the new timespan. The duration is treated as if it was the original duration applied at the time of mute.")]
        public async Task<DiscordCommandResult> UpdateInfractionAsync(
            [Description("The ID value of the infraction.")]
            string infractionId,
            [Description("The new timespan to be applied.")]
            TimeSpan newDuration)
        {
            await _infractionService.UpdateTimedInfractionDurationAsync(infractionId, newDuration);
            return Confirmation();
        }

        private string FormatInfractionCounts(InfractionType type, int num)
        {
            if (num != 0)
            {
                return num > 1
                    ? type.ToString() + "s"
                    : type.ToString();
            }

            return type.ToString() + "s";
        }

        private static string GetEmojiForInfractionType(InfractionType infractionType)
        {
            return infractionType switch
            {
                InfractionType.Note => "📝",
                InfractionType.Warn => "⚠️",
                InfractionType.Mute => "🔇",
                InfractionType.Kick => "👢",
                InfractionType.Ban => "🔨",
                _ => "❔"
            };
        }
    }
}