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
using Doraemon.Data.Services;
using Doraemon.Common;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Modules
{
    [Name("Moderation")]
    [Summary("Provides multiple utilities when dealing with users.A")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public InfractionService _infractionService;
        public ModerationModule
        (
            InfractionService infractionService,
            DoraemonContext doraemonContext,
            DiscordSocketClient client
        )
        {
            _infractionService = infractionService;
            _doraemonContext = doraemonContext;
            _client = client;
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
        [Command("purge")]
        [Alias("clean")]
        [Summary("Mass-deletes messages from the channel ran-in.")]
        public async Task PurgeChannelAsync(
            [Summary("The number of messages to purge")]
                int amount)
        {
            if (!(Context.Channel is IGuildChannel channel))
            {
                throw new InvalidOperationException($"The channel that the command is ran in must be a guild channel.");
            }
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0)
            {
                return;
            }
            var messages = await Context.Channel.GetMessagesAsync(clampedCount).FlattenAsync();
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
        }
        [Command("purge")]
        [Alias("clean")]
        [Summary("Mass-deletes messages from the channel ran-in.")]
        public async Task PurgeChannelAsync(
            [Summary("The number of messages to purge")]
                int amount,
            [Summary("The user whose messages to delete")]
                IGuildUser user)
        {
            if (!(Context.Channel is IGuildChannel guildChannel))
            {
                throw new InvalidOperationException($"The channel that the command is ran in must be a guild channel.");
            }
            var channel = Context.Channel as ITextChannel;
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0)
            {
                return;
            }
            var messages = (await channel.GetMessagesAsync(100).FlattenAsync()).Where(x => x.Author.Id == user.Id)
                .Take(clampedCount);
            await channel.DeleteMessagesAsync(messages);
        }
        [Command("kick")]
        [Summary("Kicks a user from the guild.")]
        public async Task KickUserAsync(
            [Summary("The user to be kicked.")]
                SocketGuildUser user,
            [Summary("The reason for the kick.")]
                [Remainder] string reason)
        {
            if (!Context.User.CanModerate(user))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId); // Only time we manually send the message because InfractionType.Kick doesn't exist.
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
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Warn, reason, null);
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id, Context.User.Id);
            try
            {
                await dmChannel.SendMessageAsync($"You were warned in {Context.Guild.Name} for {reason}. You currently have {infractions.Count()} current infractions.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
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
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            if (!Context.User.CanModerate((SocketGuildUser)member))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            var dmChannel = await member.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were banned from {Context.Guild.Name}. Reason: {reason}.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
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
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were banned from {Context.Guild.Name}. Reason: {reason}.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
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
            var humanizedDuration = duration.Humanize();
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Mute, reason, duration);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were muted in {Context.Guild.Name}. Reason: {reason}\nDuration: {duration.Humanize()}");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
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
            var counts = await _infractionService.FetchUserInfractionsAsync(userId, _client.CurrentUser.Id);
            var noNotes = counts
                .Where(x => x.Type != InfractionType.Note);
            if (noNotes.Count() == 0)
            {
                return;
            }
            await Context.AddConfirmationAsync();
            var user = Context.Guild.GetUser(userId);
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            if ((Context.Channel as IGuildChannel).IsPublic())
            {
                if (noNotes.Count() % 3 == 0)
                {
                    var embed = new EmbedBuilder()
                         .WithTitle($"Multiple Infractions Notice")
                         .WithColor(Color.DarkRed)
                         .WithDescription($"{user.Mention}You have amassed {noNotes.Count()} infractions. As such, you have been muted for 6 hours.")
                         .WithFooter($"Please contact Staff if you have questions!")
                         .Build();
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    try
                    {
                        await dmChannel.SendMessageAsync(embed: embed);
                    }
                    catch (HttpException ex) when (ex.DiscordCode == 50007)
                    {
                        await modLog.SendMessageAsync($"I was unable to DM the user about the above infraction.");
                    }
                }
                return;
            }
            if (!noNotes.Any())
            {
                return;
            }
            if (noNotes.Count() % 3 == 0)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Multiple Infractions Notice")
                    .WithColor(Color.DarkRed)
                    .WithDescription($"You have amassed {noNotes.Count()} infractions.")
                    .WithFooter($"Please contact Staff if you have questions!")
                    .Build();
                await ReplyAsync(embed: embed);
            }
        }
    }
}
