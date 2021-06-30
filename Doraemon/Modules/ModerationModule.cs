using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Doraemon;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Moderation;
using Doraemon.Services.Core;
using Doraemon.Common;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Doraemon.Modules
{
    [Name("Moderation")]
    [Summary("Provides multiple utilities when dealing with users.A")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DiscordSocketClient _client;
        public InfractionService _infractionService;
        public AuthorizationService _authorizationService;
        public ModerationModule
        (
            InfractionService infractionService,
            DiscordSocketClient client,
            AuthorizationService authorizationService
        )
        {
            _infractionService = infractionService;
            _client = client;
            _authorizationService = authorizationService;
        }
        [Command("note")]
        [Summary("Applies a note to a user's moderation record.")]
        public async Task ApplyNoteAsync(
            [Summary("The user the note will be referenced to.")]
                SocketGuildUser user,
            [Summary("The note's content.")]
                [Remainder] string note)
        {
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Note, note, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }
        [Command("kick")]
        [Summary("Kicks a user from the guild.")]
        public async Task KickUserAsync(
            [Summary("The user to be kicked.")]
                SocketGuildUser user,
            [Summary("The reason for the kick.")]
                [Remainder] string reason)
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.InfractionCreate);
            if (!Context.User.CanModerate(user))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId); // Only time we manually send the message because InfractionType.Kick doesn't exist.
            await modLog.SendInfractionLogMessageAsync(reason, Context.User.Id, user.Id, "Kick", _client);
            await user.KickAsync(reason);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }
        [Command("warn")]
        [Summary("Warns the user for the given reason.")]
        public async Task WarnUserAsync(
            [Summary("The user to warn.")]
                SocketGuildUser user,
            [Summary("The reason for the warn.")]
                [Remainder] string reason)
        {
            if (!Context.User.CanModerate(user))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Warn, reason, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }
        [Command("ban")]
        [Summary("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Summary("The user to be banned.")]
                IUser member,
            [Summary("The reason for the ban.")]
                [Remainder] string reason)
        {
            var ban = await Context.Guild.GetBanAsync(member);
            if (ban != null)
            {
                throw new InvalidOperationException("User is already banned.");
            }
            if (!Context.User.CanModerate((SocketGuildUser)member))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            await _infractionService.CreateInfractionAsync(member.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason, null);
            await ConfirmAndReplyWithCountsAsync(member.Id);
        }
        [Command("ban")]
        [Priority(10)]
        [Summary("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Summary("The user to be banned.")]
                ulong member,
            [Summary("The reason for the ban.")]
                [Remainder] string reason)
        {
            var user = await _client.Rest.GetUserAsync(member);
            var ban = await Context.Guild.GetBanAsync(user);
            if (ban != null)
            {
                throw new InvalidOperationException("User is already banned.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("tempban")]
        [Summary("Temporarily bans a user for the given amount of time.")]
        public async Task TempbanUserAsync(
            [Summary("The user to ban.")]
                SocketGuildUser user,
            [Summary("The duration of the ban.")]
                TimeSpan duration,
            [Summary("The reason for the ban.")]
                [Remainder] string reason)
        {
            var ban = await Context.Guild.GetBanAsync(user);
            if(ban is not null)
            {
                throw new InvalidOperationException("The user provided is already banned.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason, duration);
        }
        // We make this Async so that way if a large amount of ID's are passed, it doesn't block the gateway task.
        [Command("massban", RunMode = RunMode.Async)]
        [Summary("Bans all the ID's given.")]
        public async Task MassbanIDsAsync(
            [Summary("The IDs to ban from the guild.")]
                params ulong[] ids)
        {
            await ReplyAsync("Please do not run the command again, the massban will start in 1 second.");
            foreach (var id in ids)
            {
                await Task.Delay(1000);
                await Context.Guild.AddBanAsync(id, 7, "Massban");
            }
            await Context.AddConfirmationAsync();
        }
        [Command("unban")]
        [Summary("Rescinds an active ban on a user in the current guild.")]
        public async Task UnbanUserAsync(
            [Summary("The ID of the user to be unbanned.")]
                ulong userID,
            [Summary("The reason for the unban.")]
                [Remainder] string reason = null)
        {
            var user = await Context.Guild.GetBanAsync(userID);
            if (user is null)
            {
                throw new ArgumentException("The user provided is not currently banned.");
            }
            var unbanInfraction = await _infractionService.FetchInfractionForUserAsync(userID, InfractionType.Ban);
            await _infractionService.RemoveInfractionAsync(unbanInfraction.Id, reason ?? "Not specified", Context.User.Id, true);
            await ConfirmAndReplyWithCountsAsync(userID);
        }
        [Command("mute", RunMode = RunMode.Async)]
        [Summary("Mutes a user for the given duration.")]
        public async Task MuteUserAsync(
            [Summary("The user to be muted.")]
                SocketGuildUser user,
            [Summary("The duration of the mute.")]
                TimeSpan duration,
            [Summary("The reason for the mute.")]
                [Remainder] string reason)
        {
            if (!Context.User.CanModerate(user))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            var role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == muteRoleName);
            if (user.Roles.Contains(role))
            {
                throw new InvalidOperationException($"The user is already muted.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Mute, reason, duration);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }
        [Command("unmute")]
        [Summary("Unmutes a currently muted user.")]
        public async Task UnmuteUserAsync(
            [Summary("The user to be unmuted.")]
                SocketGuildUser user,
            [Summary("The reason for the unmute.")]
                [Remainder] string reason = null)
        {
            if (!Context.User.CanModerate(user))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            var infraction = await _infractionService.FetchInfractionForUserAsync(user.Id, InfractionType.Mute);
            await _infractionService.RemoveInfractionAsync(infraction.Id, reason ?? "Not specified", Context.User.Id, true);
            await Context.AddConfirmationAsync();
        }
        private async Task ConfirmAndReplyWithCountsAsync(ulong userId)
        {
            await Context.AddConfirmationAsync();
            if((Context.Channel as IGuildChannel).IsPublic())
            {
                return;
            }
            var counts = await _infractionService.FetchUserInfractionsAsync(userId, _client.CurrentUser.Id);
            var builder = new StringBuilder();
            var notes = counts
                .Where(x => x.Type == InfractionType.Note)
                .Where(x => x.SubjectId == userId)
                .ToList();
            var warns = counts
                .Where(x => x.Type == InfractionType.Warn)
                .Where(x => x.SubjectId == userId)
                .ToList();
            var subjectUser = await Context.Client.Rest.GetUserAsync(userId);
            builder.AppendLine($"**Notes for {subjectUser.GetFullUsername()}**");
            foreach(var note in notes)
            {
                var moderatorUser = await Context.Client.Rest.GetUserAsync(note.ModeratorId);
                builder.AppendLine($"{Format.Bold(note.Id)} **- 📝Note - Moderator:** {Format.Bold(moderatorUser.GetFullUsername())}\nReason: {note.Reason}");
            }
            builder.AppendLine();
            builder.AppendLine($"**Warns for {subjectUser.GetFullUsername()}**");
            foreach(var warn in warns)
            {
                var moderatorUser = await Context.Client.Rest.GetUserAsync(warn.ModeratorId);
                builder.AppendLine($"{Format.Bold(warn.Id)} **- ⚠️Warn - Moderator:** {Format.Bold(moderatorUser.GetFullUsername())}\nReason: {warn.Reason}");
            }
            var embed = new EmbedBuilder()
                .WithTitle($"Infractions for {subjectUser.GetFullUsername()}")
                .WithDescription(builder.ToString())
                .WithColor(Color.DarkMagenta)
                .WithFooter(subjectUser.GetFullUsername(), subjectUser.GetDefiniteAvatarUrl())
                .Build();
            await ReplyAsync(embed: embed);

        }
    }
}
