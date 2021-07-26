using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.TypeReaders;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Modules
{
    [Name("Moderation")]
    [RequireContext(ContextType.Guild)]
    [Summary("Provides multiple utilities when dealing with users.A")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly AuthorizationService _authorizationService;
        private readonly DiscordSocketClient _client;
        private readonly InfractionService _infractionService;

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

        private static DoraemonConfiguration DoraemonConfig { get; } = new();

        [Command("note")]
        [Summary("Applies a note to a user's moderation record.")]
        public async Task ApplyNoteAsync(
            [Summary("The user the note will be referenced to.")]
                SocketGuildUser user,
            [Summary("The note's content.")] [Remainder]
                string note)
        {
            RequireHigherRank(Context.User, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id,
                InfractionType.Note, note, false, null);
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
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return;
            var messages = await Context.Channel.GetMessagesAsync(clampedCount).FlattenAsync();
            var messagesToDelete = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messagesToDelete);
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
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var channel = Context.Channel as ITextChannel;
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return;
            var messages = (await channel.GetMessagesAsync().FlattenAsync()).Where(x => x.Author.Id == user.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14)
                .Take(clampedCount);
            await channel.DeleteMessagesAsync(messages);
        }

        [Command("kick")]
        [Summary("Kicks a user from the guild.")]
        public async Task KickUserAsync(
            [Summary("The user to be kicked.")] 
                UserOrMessageAuthor user,
            [Summary("The reason for the kick.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetUser(user.UserId);
            if (gUser is null)
            {
                await ReplyAsync($"User provided is not currently in the guild.");
                return;
            }
            _authorizationService.RequireClaims(ClaimMapType.InfractionCreate);
            RequireHigherRank(Context.User, gUser);
            if (!await ObtainConfirmationIfRequiredAsync(user)) return;
            var modLog =
                Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration
                    .ModLogChannelId); // Only time we manually send the message because InfractionType.Kick doesn't exist.
            await modLog.SendInfractionLogMessageAsync(reason, Context.User.Id, user.UserId, "Kick", _client);
            await gUser.KickAsync(reason);
            await ConfirmAndReplyWithCountsAsync(user.UserId);
        }

        [Command("warn")]
        [Summary("Warns the user for the given reason.")]
        public async Task WarnUserAsync(
            [Summary("The user to warn.")] 
                UserOrMessageAuthor user,
            [Summary("The reason for the warn.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetUser(user.UserId);
            if (gUser is null)
            {
                await ReplyAsync($"User provided is not currently in the guild.");
                return;
            }
            RequireHigherRank(Context.User, gUser);
            if (!await ObtainConfirmationIfRequiredAsync(user)) return;
            await _infractionService.CreateInfractionAsync(user.UserId, Context.User.Id, Context.Guild.Id,
                InfractionType.Warn, reason, false, null);
            await ConfirmAndReplyWithCountsAsync(user.UserId);
        }

        [Command("ban")]
        [Summary("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Summary("The user to be banned.")] 
                UserOrMessageAuthor member,
            [Summary("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetUser(member.UserId);
            if (gUser is not null)
            {
                RequireHigherRank(Context.User, gUser);
            }

            if (!await ObtainConfirmationIfRequiredAsync(member))
            {
                return;
            }
            var ban = await Context.Guild.GetBanAsync(member.UserId);
            if (ban != null) throw new InvalidOperationException("User is already banned.");
            await _infractionService.CreateInfractionAsync(member.UserId, Context.User.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, null);
            await ConfirmAndReplyWithCountsAsync(member.UserId);
        }

        //[Command("ban")]
        //[Priority(10)]
        //[Summary("Bans a user from the current guild.")]
        //public async Task BanUserAsync(
        //    [Summary("The user to be banned.")]
        //        ulong member,
        //    [Summary("The reason for the ban.")] [Remainder]
        //        string reason)
        //{
        //    var gUser = Context.Guild.GetUser(member);
        //    if (gUser is not null)
        //    {
        //        RequireHigherRank(Context.User, gUser);
        //    }
        //    var user = await _client.Rest.GetUserAsync(member);
        //    if (user is null)
        //        throw new InvalidOperationException($"The Id provided is not a userId.");
        //    var ban = await Context.Guild.GetBanAsync(user);
        //    if (ban != null) throw new InvalidOperationException("User is already banned.");
        //    await _infractionService.CreateInfractionAsync(user.Id, Context.User.Id, Context.Guild.Id,
        //        InfractionType.Ban, reason, false, null);
        //    await ConfirmAndReplyWithCountsAsync(user.Id);
        //}

        [Command("tempban")]
        [Summary("Temporarily bans a user for the given amount of time.")]
        public async Task TempbanUserAsync(
            [Summary("The user to ban.")] 
                UserOrMessageAuthor user,
            [Summary("The duration of the ban.")] 
                TimeSpan duration,
            [Summary("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetUser(user.UserId);
            if (gUser is not null)
            {
                RequireHigherRank(Context.User, gUser);
            }
            if (!await ObtainConfirmationIfRequiredAsync(user))
            {
                return;
            }            
            var ban = await Context.Guild.GetBanAsync(user.UserId);
            if (ban is not null) throw new InvalidOperationException("The user provided is already banned.");
            await _infractionService.CreateInfractionAsync(user.UserId, Context.User.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, duration);
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
            [Summary("The reason for the unban.")] [Remainder]
                string reason = null)
        {
            var user = await Context.Guild.GetBanAsync(userID);
            if (user == null) throw new ArgumentException("The user provided is not currently banned.");
            var infractions = await _infractionService.FetchUserInfractionsAsync(userID);
            var banInfraction = infractions
                .Where(x => x.SubjectId == userID)
                .Where(x => x.Type == InfractionType.Ban)
                .FirstOrDefault();
            if (banInfraction == null)
            {
                await Context.Guild.RemoveBanAsync(user.User.Id);
                return;
            }
            await _infractionService.RemoveInfractionAsync(banInfraction.Id, reason ?? "Not specified", Context.User.Id);
            await ConfirmAndReplyWithCountsAsync(userID);
        }

        [Command("mute", RunMode = RunMode.Async)]
        [Summary("Mutes a user for the given duration.")]
        public async Task MuteUserAsync(
            [Summary("The user to be muted.")]      
                UserOrMessageAuthor user,
            [Summary("The duration of the mute.")]
                TimeSpan duration,
            [Summary("The reason for the mute.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetUser(user.UserId);
            if (gUser is not null)
            {
                RequireHigherRank(Context.User, gUser);
            }

            if (! await ObtainConfirmationIfRequiredAsync(user))
            {
                return;
            }
            await _infractionService.CreateInfractionAsync(user.UserId, Context.User.Id, Context.Guild.Id,
                InfractionType.Mute, reason, false, duration);
            await ConfirmAndReplyWithCountsAsync(user.UserId);
        }

        [Command("unmute")]
        [Summary("Unmutes a currently muted user.")]
        public async Task UnmuteUserAsync(
            [Summary("The user to be unmuted.")] 
                SocketGuildUser user,
            [Summary("The reason for the unmute.")] [Remainder]
                string reason = null)
        {
            RequireHigherRank(Context.User, user);
            var muteRole = Context.Guild.Roles.Where(x => x.Name == muteRoleName).FirstOrDefault();
            
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var infractionToRemove = infractions
                .Where(x => x.SubjectId == user.Id)
                .Where(x => x.Type == InfractionType.Mute)
                .FirstOrDefault();
            if (infractionToRemove == null)
            {
                throw new InvalidOperationException($"The user provided does not have an active mute infraction.");
            }
            await _infractionService.RemoveInfractionAsync(infractionToRemove.Id, reason ?? "Not specified", Context.User.Id);
            await Context.AddConfirmationAsync();
        }


        private void RequireHigherRank(SocketUser user1, SocketGuildUser user2)
        {
            if ((user1 as SocketGuildUser).Hierarchy <= user2.Hierarchy)
                throw new InvalidOperationException($"⚠ Invalid Permissions");
        }

        private async Task<bool> ObtainConfirmationIfRequiredAsync(UserOrMessageAuthor entity)
        {
            if (entity.MessageId == null)
            {
                return true;
            }

            var author = await _client.Rest.GetUserAsync(entity.UserId);
            return await Context.GetUserConfirmationAsync(
                $"You provided a message ID instead of a user ID. Do you want to perform this action on **{author.GetFullUsername()}**, the message's author?");

        }
        private async Task ConfirmAndReplyWithCountsAsync(ulong userId)
        {
            await Context.AddConfirmationAsync();
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var counts = await _infractionService.FetchUserInfractionsAsync(userId);
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
            foreach (var note in notes)
            {
                var moderatorUser = await Context.Client.Rest.GetUserAsync(note.ModeratorId);
                builder.AppendLine(
                    $"{Format.Bold(note.Id)} **- 📝Note - Moderator:** {Format.Bold(moderatorUser.GetFullUsername())}\nReason: {note.Reason}");
            }

            builder.AppendLine();
            builder.AppendLine($"**Warns for {subjectUser.GetFullUsername()}**");
            foreach (var warn in warns)
            {
                var moderatorUser = await Context.Client.Rest.GetUserAsync(warn.ModeratorId);
                builder.AppendLine(
                    $"{Format.Bold(warn.Id)} **- ⚠️Warn - Moderator:** {Format.Bold(moderatorUser.GetFullUsername())}\nReason: {warn.Reason}");
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