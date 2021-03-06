using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Doraemon.Data.TypeReaders;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Server;
using Qmmands;
using RestSharp.Extensions;
using Format = Doraemon.Common.Extensions.Format;

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
        [RequireClaims(ClaimMapType.InfractionNote)]
        [Description("Applies a note to a userId's moderation record.")]
        public async Task<DiscordCommandResult> ApplyNoteAsync(
            [Description("The userId the note will be referenced to.")]
                IMember user,
            [Description("The note's content.")] [Remainder]
                string note)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Note, note, false, null);
            await ReplyWithCountsAsync(user.Id);
            return Confirmation();
        }

        [Command("purge", "clean")] // Piece of shit work properly
        [RequireClaims(ClaimMapType.InfractionPurge)]
        [Description("Mass-deletes messages from the channel ran-in.")]
        public async Task<DiscordCommandResult> PurgeChannelAsync(
            [Description("The number of messages to purge")]
                int amount)
        {
            if (!(Context.Channel is IGuildChannel channel))
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return null;
            var messages = await Context.Channel.FetchMessagesAsync(clampedCount);
            var messagesToDelete = messages.Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14).Select(x => x.Id);
            if (!await PromptAsync(new LocalMessage().WithContent($"You are attempting to purge {clampedCount} messages?")))
            {
                return null;
            }
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messagesToDelete);
            return Response($"✅ Purged {clampedCount} messages.");
        }

        [Command("purge", "clean")]
        [RequireClaims(ClaimMapType.InfractionPurge)]
        [Description("Mass-deletes messages from the channel ran-in.")]
        public async Task<DiscordCommandResult> PurgeChannelAsync(
            [Description("The number of messages to purge")]
                int amount,
            [Description("The userId whose messages to delete")]
                IMember user)
        {
            if (!(Context.Channel is IGuildChannel guildChannel))
                throw new InvalidOperationException("The channel that the command is ran in must be a guild channel.");
            var channel = Context.Channel as ITextChannel;
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0) return null;
            var messages = (await channel.FetchMessagesAsync()).Where(x => x.Author.Id == user.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Take(clampedCount)
                .Select(x => x.Id);
            if (!await PromptAsync(new LocalMessage().WithContent($"You are attempting to purge {clampedCount} messages by {Mention.User(user)}?")))
            {
                return null;
            }
            await channel.DeleteMessagesAsync(messages);
            return Response($"✅ Purged {clampedCount} messages sent by {Format.Bold(user.Tag)}");
        }

        [Command("kick")]
        [RequireClaims(ClaimMapType.InfractionKick)]
        [Description("Kicks a userId from the guild.")]
        public async Task<DiscordCommandResult> KickUserAsync(
            [Description("The userId to be kicked.")]
                IMember user,
            [Description("The reason for the kick.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.GuildId, InfractionType.Kick, reason, false, null);
            return Confirmation();
        }

        [Command("warn")]
        [RequireClaims(ClaimMapType.InfractionWarn)]
        [Description("Warns the userId for the given reason.")]
        public async Task<DiscordCommandResult> WarnUserAsync(
            [Description("The userId to warn.")]
                IMember user,
            [Description("The reason for the warn.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Warn, reason, false, null);
            await ReplyWithCountsAsync(user.Id);
            return Confirmation();
        }

        [Command("ban")]
        [RequireClaims(ClaimMapType.InfractionBan)]
        [Description("Bans a userId from the current guild.")]
        public async Task<DiscordCommandResult> BanUserAsync(
            [Description("The userId to be banned.")]
                IMember member,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            var ban = await Context.Guild.FetchBanAsync(member.Id);
            if (ban != null) throw new InvalidOperationException("User is already banned.");
            await _infractionService.CreateInfractionAsync(member.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, null);
            await ReplyWithCountsAsync(member.Id);
            return Confirmation();
        }

        [Command("ban")]
        [RequireClaims(ClaimMapType.InfractionBan)]
        [Priority(10)]
        [Description("Bans a userId from the current guild.")]
        public async Task<DiscordCommandResult> BanUserAsync(
            [Description("The userId to be banned.")]
                Snowflake memberId,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetMember(memberId);
            var user = await Bot.FetchUserAsync(memberId);
            if (user is null)
                throw new InvalidOperationException($"The Id provided is not a userId.");
            if (gUser == null)
            {
                if(!await PromptAsync(new LocalMessage().WithContent($"The userId ({user.Tag}) was not found inside the guild. Would you like to forceban them?")))
                {
                    return null;
                }
            }
            else
            {
                await BanUserAsync(gUser, reason);
                return null;
            }
            var ban = await Context.Guild.FetchBanAsync(memberId);
            if (ban != null)
                throw new InvalidOperationException("User is already banned.");
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, null);
            await ReplyWithCountsAsync(user.Id);
            return Confirmation();
        }

        [Command("tempban")]
        [RequireClaims(ClaimMapType.InfractionBan)]
        [Description("Temporarily bans a userId for the given amount of time.")]
        public async Task<DiscordCommandResult> TempbanUserAsync(
            [Description("The userId to ban.")]
                IMember user,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, duration);
            return Confirmation();
        }
        
        [Command("tempban")]
        [RequireClaims(ClaimMapType.InfractionBan)]
        [Priority(10)]
        [Description("Temporarily bans a userId for the given amount of time.")]
        public async Task<DiscordCommandResult> TempbanUserAsync(
            [Description("The userId to ban.")] 
                Snowflake userId,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var gUser = Context.Guild.GetMember(userId);
            if (gUser is not null)
            {
                await TempbanUserAsync(gUser, duration, reason);
                return null;
            }
            var ban = await Context.Guild.FetchBanAsync(userId);
            if (ban is not null) throw new InvalidOperationException("The userId provided is already banned.");
            await _infractionService.CreateInfractionAsync(userId, Context.Author.Id, Context.Guild.Id,
                InfractionType.Ban, reason, false, duration);
            return Confirmation();
        }

        // We make this Async so that way if a large amount of ID's are passed, it doesn't block the gateway task.
        [Command("massban")]
        [RequireClaims(ClaimMapType.InfractionBan)]
        [RunMode(RunMode.Parallel)]
        [Description("Bans all the ID's given.")]
        public async Task<DiscordCommandResult> MassbanIDsAsync(
            [Description("The IDs to ban from the guild.")]
            params ulong[] ids)
        {
            // Since we never use the service, gotta check here :smirk:
            if (ids.Count() > 100)
            {
                throw new Exception("You can't massban more than 100 users at once.");
            }
            _authorizationService.RequireClaims(ClaimMapType.InfractionBan);
            using (var scope = Context.Bot.Services.CreateScope())
            {
                await Response("Please provide a ban reason, or `cancel` to cancel the command.");
                var reason = await Context.WaitForMessageAsync(x => x.Member.Id == Context.Author.Id, TimeSpan.FromSeconds(30));
                if (reason == null)
                {
                    return Response("A reason was not provided, command cancelled.");
                }

                if (reason.Message.Content.ToLower() == "cancel")
                {
                    return Response("Cancellation received.");
                }
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var banMessage = await Response($"Beginning the massban, 0/{ids.Length}");
                int currentBannedUsers = 0;
                List<Snowflake> failedBans = new();
                foreach (var id in ids)
                {
                    await Task.Delay(1000);
                    await infractionRepository.CreateAsync(new InfractionCreationData
                    {
                        Id = DatabaseUtilities.ProduceId(),
                        SubjectId = id,
                        ModeratorId = Context.Author.Id,
                        Reason = reason.Message.Content,
                        Type = InfractionType.Ban
                    });
                    try
                    {
                        await Context.Guild.CreateBanAsync(id, reason.Message.Content, 7);
                        await banMessage.ModifyAsync(x =>
                        {
                            x.Content = $"Banning... {currentBannedUsers++}/{ids.Length}";
                        });
                    }
                    catch (RestApiException)
                    {
                        failedBans.Add(id);
                    }
                }

                if (failedBans.Any())
                {
                    await banMessage.ModifyAsync(x => x.Content = $"Successfully banned {currentBannedUsers}/{ids.Length}. Failed: {failedBans.Humanize()}");
                }
                else
                {
                    await banMessage.ModifyAsync(x => x.Content = $"Successfully banned {currentBannedUsers}/{ids.Length}.");
                }

                var logChannel = Context.Guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId) as ITextChannel;
                await logChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"`{DateTimeOffset.UtcNow}`⚒**{Context.Author.Tag}**(`{Context.Author.Id}`) massbanned {currentBannedUsers} users. Reason:\n```{reason.Message.Content}```"));
                return Confirmation();
            }
            
        }

        [Command("unban")]
        [RequireClaims(ClaimMapType.InfractionDelete)]
        [Description("Rescinds an active ban on a userId in the current guild.")]
        public async Task<DiscordCommandResult> UnbanUserAsync(
            [Description("The ID of the userId to be unbanned.")]
            Snowflake userId,
                [Description("The reason for the unban.")] [Remainder]
            string reason = null)
        {
            var user = await Context.Guild.FetchBanAsync(userId);
            if (user == null) throw new ArgumentException("The userId provided is not currently banned.");
            var infractions = await _infractionService.FetchUserInfractionsAsync(userId);
            var banInfraction = infractions
                .Where(x => x.SubjectId == userId)
                .Where(x => x.Type == InfractionType.Ban)
                .FirstOrDefault();
            if (banInfraction == null)
            {
                await Context.Guild.DeleteBanAsync(user.User.Id);
                return Confirmation();
            }

            await _infractionService.RemoveInfractionAsync(banInfraction.Id, reason ?? "Not specified", Context.Author.Id);
            await ReplyWithCountsAsync(user.User.Id);
            return Confirmation();
        }

        [Command("mute")]
        [RequireClaims(ClaimMapType.InfractionMute)]
        [Description("Mutes a userId for the given duration.")]
        public async Task<DiscordCommandResult> MuteUserAsync(
            [Description("The userId to be muted.")]
                IMember user,
            [Description("The duration of the mute.")]
                TimeSpan duration,
            [Description("The reason for the mute.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, user);
            await _infractionService.CreateInfractionAsync(user.Id, Context.Author.Id, Context.Guild.Id,
                InfractionType.Mute, reason, false, duration);
            await ReplyWithCountsAsync(user.Id);
            return Confirmation();
        }

        [Command("nick", "n", "nickname")]
        [Description("Modifies a guild users nickname. Note that the InfractionCreate claim is required due to the fact that it can be considered a \"warning\" of sorts if you have to change a members nickname. No infraction is created though.")]
        [RequireClaims(ClaimMapType.InfractionNote)]
        public async Task<DiscordCommandResult> ModifyDiscordNicknameAsync(
            [Description("The user whose nickname to change.")]
                IMember member,
            [Description("The new nickname.")] [Remainder]
                string nickname)
        {
            RequireHigherRank(Context.Author, member);
            _authorizationService.RequireClaims(ClaimMapType.InfractionNote);
            await member.ModifyAsync(x => x.Nick = nickname);
            return Confirmation();
        }
        [Command("unmute")]
        [RequireClaims(ClaimMapType.InfractionDelete)]
        [Description("Unmutes a currently muted userId.")]
        public async Task<DiscordCommandResult> UnmuteUserAsync(
            [Description("The userId to be unmuted.")]
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
                throw new InvalidOperationException($"The userId provided does not have an active mute infraction.");
            }

            await _infractionService.RemoveInfractionAsync(infractionToRemove.Id, reason ?? "Not specified", Context.Author.Id);
            return Confirmation();
        }


        private void RequireHigherRank(IMember user1, IMember user2)
        {
            if (user1.GetHierarchy() <= user2.GetHierarchy())
                throw new Exception($"⚠️Executing userId is not a higher rank than the subject.");
            if (Context.CurrentMember.GetHierarchy() <= user2.GetHierarchy())
                throw new Exception($"⚠️The bot must be higher ranked than the subject to moderate them.");
        }

        private async Task ReplyWithCountsAsync(Snowflake userId)
        {
            if ((Context.Channel as IGuildChannel).IsPublic()) return;
            var user = await Bot.FetchUserAsync(userId);
            var counts = await _infractionService.FetchUserInfractionsAsync(userId);
            var notes = counts.Where(x => x.Type == InfractionType.Note).ToList();
            var warns = counts.Where(x => x.Type == InfractionType.Warn).ToList();
            var bans = counts.Where(x => x.Type == InfractionType.Ban).ToList();
            var mutes = counts.Where(x => x.Type == InfractionType.Mute).ToList();
            var kicks = counts.Where(x => x.Type == InfractionType.Kick).ToList();
            if (counts.Count() == 0)
                return;
            if (counts.Count() < 3)
                return;
            var embed = new LocalEmbed()
                .WithColor(DColor.Orange)
                .WithDescription($"{user.Tag} has {notes.Count} {FormatInfractionCounts(InfractionType.Note, notes.Count)}, {warns.Count} {FormatInfractionCounts(InfractionType.Warn, warns.Count)}, {mutes.Count} {FormatInfractionCounts(InfractionType.Mute, mutes.Count)}, {kicks.Count} {FormatInfractionCounts(InfractionType.Kick, kicks.Count)}, and {bans.Count} {FormatInfractionCounts(InfractionType.Ban, bans.Count)}.")
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