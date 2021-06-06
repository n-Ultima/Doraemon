using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Doraemon.Common.Attributes;
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
    [Summary("Provides multiple utilities when dealing with me")]
    [RequireStaff]
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
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLog.SendInfractionLogMessageAsync(reason, Context.User.Id, user.Id, "Kick");
            await user.KickAsync(reason);
            await Context.AddConfirmationAsync();
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
            var infractions = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.SubjectId == user.Id)
                .ToListAsync();
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Warn, reason);
            await modLog.SendInfractionLogMessageAsync(reason, Context.User.Id, user.Id, "Warn");
            try
            {
                await dmChannel.SendMessageAsync($"You were warned in {Context.Guild.Name} for {reason}. You currently have {infractions.Count} infractions.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
            await Context.AddConfirmationAsync();
        }
        [Command("ban")]
        [Summary("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Summary("The user to be banned.")]
                IUser member,
            [Summary("The reason for the ban.")]
                [Remainder] string reason)
        {
            var user = await _client.Rest.GetUserAsync(member.Id);
            if (user is null)
            {
                await ReplyAsync("The user is null.");
                return;
            }
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            if (!Context.User.CanModerate((SocketGuildUser)member))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            await modLog.SendInfractionLogMessageAsync(reason, Context.User.Id, user.Id, "Ban");
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were banned from {Context.Guild.Name}. Reason: {reason}.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason);
            await Context.Guild.AddBanAsync(user, 0, reason);
        }
        [Command("tempban", RunMode = RunMode.Async)]
        [Summary("Temporarily bans a user from the guild.")]
        public async Task TempBanUserAsync(
            [Summary("The user to be banned temporarily.")]
                IUser member,
            [Summary("The duration of the ban.")]
                string duration,
            [Summary("The reason for the ban.")]
                [Remainder] string reason)
        {
            var user = await _client.Rest.GetUserAsync(member.Id);
            if (!Context.User.CanModerate((SocketGuildUser)member))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            char minute = 'm';
            char day = 'd';
            char hour = 'h';
            char second = 's';
            char week = 'w';
            var BanTimer = new string(duration.Where(char.IsDigit).ToArray());
            if (minute.ToString().Any(duration.Contains) && day.ToString().Any(duration.Contains) && hour.ToString().Any(duration.Contains) && second.ToString().Any(duration.Contains) && week.ToString().Any(duration.Contains))
            {
                await Context.Channel.SendMessageAsync("You cannot pass multiple Time formats.");
                return;
            }
            if (BanTimer.Length == 0)
            {
                return;
            }
            var Timer = Convert.ToInt32(BanTimer);
            if (minute.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Bans.Add(new Ban { Guild = Context.Guild, User = (SocketGuildUser)member, End = DateTime.Now + TimeSpan.FromMinutes(Timer) });
            }
            else if (day.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Bans.Add(new Ban { Guild = Context.Guild, User = (SocketGuildUser)member, End = DateTime.Now + TimeSpan.FromDays(Timer) });
            }
            else if (second.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Bans.Add(new Ban { Guild = Context.Guild, User = (SocketGuildUser)member, End = DateTime.Now + TimeSpan.FromSeconds(Timer) });
            }
            else if (hour.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Bans.Add(new Ban { Guild = Context.Guild, User = (SocketGuildUser)member, End = DateTime.Now + TimeSpan.FromHours(Timer) });
            }
            else if (week.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Bans.Add(new Ban { Guild = Context.Guild, User = (SocketGuildUser)member, End = DateTime.Now + TimeSpan.FromDays(Timer * 7) });
            }
            else
            {
                throw new ArgumentException("The duration provided is not valid.");
            }
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLog.SendInfractionLogMessageAsync(reason + $"Timer: {duration}", Context.User.Id, user.Id, "Ban");
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were temporarily banned from {Context.Guild.Name}. Reason: {reason}\nDuration: {duration}");
            }
            catch(HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, reason);
            await Context.Guild.AddBanAsync(user, 0, reason);
            await Context.AddConfirmationAsync();
        }
        // We make this Async so that way if a large amount of ID's are passed, it doesn't block the gateway task.
        [Command("massban", RunMode = RunMode.Async)]
        [Summary("Bans all the ID's given.")]
        public async Task MassbanIDsAsync(
            [Summary("The IDs to ban from the guild.")]
                params ulong[] ids)
        {
            await ReplyAsync("Please do not run the command again, the massban will start in 1 second.");
            foreach(var id in ids)
            {
                await Task.Delay(1000);
                await Context.Guild.AddBanAsync(id, options: new RequestOptions()
                {
                    AuditLogReason = "Massban."
                });
                await _infractionService.CreateInfractionAsync(id, Context.User.Id, Context.Guild.Id, InfractionType.Ban, "Massban.");
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
            if(user is null)
            {
                throw new ArgumentException("The user provided is not currently banned.");
            }
            await Context.Guild.RemoveBanAsync(userID);
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var unbanInfraction = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.SubjectId == userID)
                .SingleOrDefaultAsync();
            await _infractionService.RemoveInfractionAsync(unbanInfraction.Id);
            await modLog.SendInfractionLogMessageAsync(reason ?? "No reason specified", Context.User.Id, userID, "Unban");
            await Context.AddConfirmationAsync();
        }
        [Command("mute", RunMode = RunMode.Async)]
        [Summary("Mutes a user for the given duration.")]
        public async Task MuteUserAsync(
            [Summary("The user to be muted.")]
                SocketGuildUser user,
            [Summary("The duration of the mute.")]
                string duration,
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
                await Context.Channel.SendErrorMessageAsync("Error", "User is already muted.");
                return;
            }
            char minute = 'm';
            char day = 'd';
            char hour = 'h';
            char second = 's';
            char week = 'w';
            var MuteTimer = new string(duration.Where(char.IsDigit).ToArray());
            if (minute.ToString().Any(duration.Contains) && day.ToString().Any(duration.Contains) && hour.ToString().Any(duration.Contains) && second.ToString().Any(duration.Contains))
            {
                await Context.Channel.SendMessageAsync("You cannot pass multiple Time formats.");
                return;
            }
            if (MuteTimer.Length == 0)
            {
                return;
            }
            var Timer = Convert.ToInt32(MuteTimer);
            if (minute.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = (SocketGuildUser)user, End = DateTime.Now + TimeSpan.FromMinutes(Timer), Role = role });
            }
            else if (day.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = (SocketGuildUser)user, End = DateTime.Now + TimeSpan.FromDays(Timer), Role = role });
            }
            else if (second.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = (SocketGuildUser)user, End = DateTime.Now + TimeSpan.FromSeconds(Timer), Role = role });
            }
            else if (hour.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = (SocketGuildUser)user, End = DateTime.Now + TimeSpan.FromHours(Timer), Role = role });
            }
            else if (week.ToString().Any(duration.ToLower().Contains))
            {
                CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = (SocketGuildUser)user, End = DateTime.Now + TimeSpan.FromDays(Timer * 7), Role = role });
            }
            else
            {
                Console.WriteLine("The duration provided is not valid.");
            }
            await user.AddRoleAsync(role);
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLog.SendInfractionLogMessageAsync(reason + $" Duration: {duration}", Context.User.Id, user.Id, "Mute");
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id, InfractionType.Mute, reason);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync($"You were muted in {Context.Guild.Name}. Reason: {reason}\nDuration: {duration}");
            }
            catch(HttpException ex) when (ex.DiscordCode == 50007)
            {
                await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
            }
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
            var modLog = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLog.SendInfractionLogMessageAsync(reason ?? "No reason provided", Context.User.Id, user.Id, "Mute rescinded.");
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            if (!user.Roles.Contains(role))
            {
                throw new ArgumentException("The user provided is not currently muted.");
            }
            await user.RemoveRoleAsync(role);
            var infraction = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.SubjectId == user.Id)
                .SingleOrDefaultAsync();
            await _infractionService.RemoveInfractionAsync(infraction.Id);
            await Context.AddConfirmationAsync();
        }
    }
}
