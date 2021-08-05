using System;
using System.Linq;
using System.Text;
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
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.Server;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Moderation")]
    [Description("Provides multiple utilities when dealing with users.")]
    public class ModerationModule : DoraemonGuildModuleBase
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly AuthorizationService _authorizationService;
        private readonly InfractionService _infractionService;

        public ModerationModule
        (
            InfractionService infractionService,
            AuthorizationService authorizationService
        )
        {
            _infractionService = infractionService;
            _authorizationService = authorizationService;
        }

        private static DoraemonConfiguration DoraemonConfig { get; } = new();

        [Command("note")]
        [Description("Applies a note to a user's moderation record.")]
        public async Task ApplyNoteAsync(
            [Description("The user the note will be referenced to.")]
                IMember user,
            [Description("The note's content.")] [Remainder]
                string note)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Note, note, false, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("purge", "clean")]
        [Description("Mass-deletes messages from the channel ran-in.")]
        public async Task PurgeChannelAsync(
            [Description("The number of messages to purge")]
            int amount)
        {
            if (!(Context.Channel is IGuildChannel channel))
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return;
            var messages = await Context.Channel.FetchMessagesAsync(clampedCount);
            var messagesToDelete = messages.Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14).Select(x => x.Id);
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messagesToDelete);
        }

        [Command("purge", "clean")]
        [RequireAuthorGuildPermissions(Permission.ManageMessages)]
        [Description("Mass-deletes messages from the channel ran-in.")]
        public async Task PurgeChannelAsync(
            [Description("The number of messages to purge")]
            int amount,
            [Description("The user whose messages to delete")]
            IMember user)
        {
            if (!(Context.Channel is IGuildChannel guildChannel))
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var channel = Context.Channel as ITextChannel;
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return;
            var messages = (await channel.FetchMessagesAsync()).Where(x => x.Author.Id == user.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Take(clampedCount)
                .Select(x => x.Id);
            await channel.DeleteMessagesAsync(messages);
        }

        [Command("kick")]
        [Description("Kicks a user from the guild.")]
        public async Task KickUserAsync(
            [Description("The user to be kicked.")]
                IMember user,
            [Description("The reason for the kick.")] [Remainder]
                string reason)
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionCreate);
            RequireHigherRank(Context.Author, user);
            // Only time we manually send the message because InfractionType.Kick doesn't exist.
            var modLog = Context.Guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLog.SendInfractionLogMessageAsync(reason, Context.Author.Id, user.Id, "Kick", Bot);
            await user.KickAsync(new DefaultRestRequestOptions()
            {
                Reason = reason
            });
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("warn")]
        [Description("Warns the user for the given reason.")]
        public async Task WarnUserAsync(
            [Description("The user to warn.")] IMember user,
            [Description("The reason for the warn.")] [Remainder]
            string reason)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Warn, reason, false, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("ban")]
        [Description("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Description("The user to be banned.")]
            IMember member,
            [Description("The reason for the ban.")] [Remainder]
            string reason)
        {
            var ban = await Context.Guild.FetchBanAsync(member.Id);
            if (ban != null) throw new InvalidOperationException("User is already banned.");
            await _infractionService.CreateInfractionAsync(member.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, null);
            await ConfirmAndReplyWithCountsAsync(member.Id);
        }

        [Command("ban")]
        [Priority(10)]
        [Description("Bans a user from the current guild.")]
        public async Task BanUserAsync(
            [Description("The user to be banned.")]
                Snowflake member,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetMember(member);
            if (gUser is not null)
            {
                RequireHigherRank(Context.Author, gUser);
            }

            var user = await Bot.FetchUserAsync(member);
            if (user is null)
                throw new InvalidOperationException($"The Id provided is not a userId.");
            var ban = await Context.Guild.FetchBanAsync(member);
            if (ban != null)
                throw new InvalidOperationException("User is already banned.");
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, null);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("tempban")]
        [Description("Temporarily bans a user for the given amount of time.")]
        public async Task TempbanUserAsync(
            [Description("The user to ban.")]
                IMember user,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var ban = await Context.Guild.FetchBanAsync(user.Id);
            if (ban is not null) throw new InvalidOperationException("The user provided is already banned.");
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, duration);
        }

        [Command("tempban")]
        [Priority(10)]
        [Description("Temporarily bans a user for the given amount of time.")]
        public async Task TempbanUserAsync(
            [Description("The user to ban.")] 
                Snowflake user,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetMember(user);
            if (gUser is not null)
            {
                RequireHigherRank(Context.Author, gUser);
            }
            

            var ban = await Context.Guild.FetchBanAsync(user);
            if (ban is not null) throw new InvalidOperationException("The user provided is already banned.");
            await _infractionService.CreateInfractionAsync(user, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, duration);
        }

        // We make this Async so that way if a large amount of ID's are passed, it doesn't block the gateway task.
        [Command("massban")]
        [RequireAuthorGuildPermissions(Permission.BanMembers)]
        [RunMode(RunMode.Parallel)]
        [Description("Bans all the ID's given.")]
        public async Task MassbanIDsAsync(
            [Description("The IDs to ban from the guild.")]
            params ulong[] ids)
        {
            await Context.Channel.SendMessageAsync(new LocalMessage().WithContent("Massban will begin in 1 minute. Please don't run the command again."));
            foreach (var id in ids)
            {
                await Task.Delay(1000);
                await Context.Guild.CreateBanAsync(id, "Massban", 7);
            }

            await Context.AddConfirmationAsync();
        }

        [Command("unban")]
        [Description("Rescinds an active ban on a user in the current guild.")]
        public async Task UnbanUserAsync(
            [Description("The ID of the user to be unbanned.")]
            Snowflake userId,
                [Description("The reason for the unban.")] [Remainder]
            string reason = null)
        {
            var user = await Context.Guild.FetchBanAsync(userId);
            if (user == null) throw new ArgumentException("The user provided is not currently banned.");
            var infractions = await _infractionService.FetchUserInfractionsAsync(userId);
            var banInfraction = infractions
                .Where(x => x.SubjectId == userId)
                .Where(x => x.Type == InfractionType.Ban)
                .FirstOrDefault();
            if (banInfraction == null)
            {
                await Context.Guild.DeleteBanAsync(user.User.Id);
                return;
            }

            await _infractionService.RemoveInfractionAsync(banInfraction.Id, reason ?? "Not specified", Context.Author.Id);
            await ConfirmAndReplyWithCountsAsync(user.User.Id);
        }

        [Command("mute")]
        [Description("Mutes a user for the given duration.")]
        public async Task MuteUserAsync(
            [Description("The user to be muted.")]
                IMember user,
            [Description("The duration of the mute.")]
                TimeSpan duration,
            [Description("The reason for the mute.")] [Remainder]
                string reason)
        {
            

            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Mute, reason, false, duration);
            await ConfirmAndReplyWithCountsAsync(user.Id);
        }

        [Command("unmute")]
        [Description("Unmutes a currently muted user.")]
        public async Task UnmuteUserAsync(
            [Description("The user to be unmuted.")]
                IMember user,
            [Description("The reason for the unmute.")] [Remainder]
                string reason = null)
        {
            RequireHigherRank(Context.Author, user);
            var muteRole = Context.Guild.Roles
                .Where(x => x.Value.Name == muteRoleName)
                .Select(x => x.Value)
                .FirstOrDefault();

            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var infractionToRemove = infractions
                .Where(x => x.SubjectId == user.Id)
                .Where(x => x.Type == InfractionType.Mute)
                .FirstOrDefault();
            if (infractionToRemove == null)
            {
                throw new InvalidOperationException($"The user provided does not have an active mute infraction.");
            }

            await _infractionService.RemoveInfractionAsync(infractionToRemove.Id, reason ?? "Not specified", Context.Author.Id);
            await Context.AddConfirmationAsync();
        }


        private void RequireHigherRank(IMember user1, IMember user2)
        {
            if (user1.GetHierarchy() <= user2.GetHierarchy())
                throw new Exception($"Executing user is not a higher rank than the subject.");
        }

        private async Task ConfirmAndReplyWithCountsAsync(Snowflake userId)
        {
            await Context.AddConfirmationAsync();
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var user = await Bot.FetchUserAsync(userId);
            var counts = await _infractionService.FetchUserInfractionsAsync(userId);
            var notes = counts.Where(x => x.Type == InfractionType.Note).ToList();
            var warns = counts.Where(x => x.Type == InfractionType.Warn).ToList();
            var bans = counts.Where(x => x.Type == InfractionType.Ban).ToList();
            var mutes = counts.Where(x => x.Type == InfractionType.Mute).ToList();
            if (counts.Count() == 0)
                return;
            if (counts.Count() < 3)
                return;
            var embed = new LocalEmbed()
                .WithColor(DColor.Orange)
                .WithDescription($"{user.Tag} has {notes.Count} {FormatInfractionCounts(InfractionType.Note, notes.Count)}, {warns.Count} {FormatInfractionCounts(InfractionType.Warn, warns.Count)}, {mutes.Count} {FormatInfractionCounts(InfractionType.Mute, mutes.Count)}, and {bans.Count} {FormatInfractionCounts(InfractionType.Ban, bans.Count)}.")
                .WithTitle("Infraction Count Notice");
            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));

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
    }
}