using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Services.Modmail;
using Qmmands;
using Serilog;

namespace Doraemon.Modules
{
    [Name("Modmail")]
    [Description("Contains all the commands used for handling modmail tickets.")]
    public class ModmailModule : DoraemonGuildModuleBase
    {
        private readonly GuildUserService _guildUserService;
        private readonly ModmailTicketService _modmailTicketService;
        private readonly AuthorizationService _authorizationService;

        public ModmailModule(AuthorizationService authorizationService,
            ModmailTicketService modmailTicketService, GuildUserService guildUserService)
        {
            _guildUserService = guildUserService;
            _authorizationService = authorizationService;
            _modmailTicketService = modmailTicketService;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        [Command("reply", "respond")]
        [RequireClaims(ClaimMapType.ModmailManage)]
        [Description("Replies to a current modmail thread.")]
        public async Task ReplyTicketAsync(
            [Description("The ticket ID to reply to.")]
                string ID,
            [Description("The response")] [Remainder] 
                string response)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketAsync(ID);
            if (modmail is null) throw new NullReferenceException("The ID provided is invalid.");
            var user = Context.Guild.GetMember(modmail.UserId);
            var highestRole = Context.Author.GetRoles().OrderByDescending(x => x.Value.Position).Select(x => x.Value).First().Name;
            if (highestRole is null) highestRole = "@everyone";
            var embed = new LocalEmbed()
                .WithAuthor(Context.Author)
                .WithColor(DColor.Green)
                .WithDescription(response)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter($"{highestRole}");
            await Bot.SendMessageAsync(modmail.DmChannelId, new LocalMessage()
                .WithEmbeds(embed));
            await Context.AddConfirmationAsync();
        }

        [Command("close", "delete")]
        [RequireClaims(ClaimMapType.ModmailManage)]
        [Description("Closes the modmail thread that the command is run inside of.")]
        public async Task<DiscordCommandResult> CloseTicketAsync()
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(Context.Channel.Id);
            if (modmail is null)
                throw new NullReferenceException("This channel is not a modmail thread.");
            var id = modmail.Id;
            var channel = Context.Channel as ITextChannel;
            await channel.DeleteAsync();
            var embed = new LocalEmbed()
                .WithTitle("Thread Closed")
                .WithColor(DColor.Red)
                .WithDescription($"{Context.Author.Mention} has closed this Modmail thread.")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(iconUrl: Context.Guild.GetIconUrl(), text: "Replying will create a new thread");
            var user = Context.Guild.GetMember(modmail.UserId);
            var modmailLogChannel = Context.Guild.GetChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId) as ITextChannel;
            try
            {
                await user.SendMessageAsync(new LocalMessage()
                    .WithEmbeds(embed));
            }
            catch (RestApiException ex)
            {
                Log.Logger.Warning(ex, "Failed DM");
                await modmailLogChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"Unable to DM {user.Tag} about the closed thread."));
            }
            var ms = new MemoryStream();
            var encoding = new UTF8Encoding(true);
            foreach (var message in await _modmailTicketService.FetchModmailMessagesAsync(modmail.Id))
            {
                var info = encoding.GetBytes(message.Content);
                ms.Write(info, 0, info.Length);
            }

            ms.Position = 0;
            await _modmailTicketService.DeleteModmailTicketAsync(id);
            await modmailLogChannel.SendMessageAsync(new LocalMessage().WithAttachments(new LocalAttachment(ms, $"{modmail.Id} - modmail ticket.txt")));
            return Confirmation();
        }

        [Command("block")]
        [RequireClaims(ClaimMapType.ModmailManage)]
        [Description("Blocks a user from creating modmail threads.")]
        public async Task BlockUserAsync(
            [Description("The user to block.")] 
                IMember user,
            [Description("The reason for the block.")] [Remainder]
                string reason)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var checkForBlock = await _guildUserService.FetchGuildUserAsync(user.Id);
            if (checkForBlock is null)
            {
                await _guildUserService.CreateGuildUserAsync(user.Id, user.Name, user.Discriminator, true);
                await Context.AddConfirmationAsync();
            }
            else
            {
                if (checkForBlock.IsModmailBlocked)
                    throw new InvalidOperationException("The user provided is already blocked.");
                await _guildUserService.UpdateGuildUserAsync(user.Id, null, null, true);
                await Context.AddConfirmationAsync();
            }
        }

        [Command("unblock")]
        [RequireClaims(ClaimMapType.ModmailManage)]
        [Description("Unblocks a user from the modmail system.")]
        public async Task UnblockUserAsync(
            [Description("The user to unblock.")] 
                IMember user,
            [Description("The reason for the unblock.")] [Remainder]
                string reason)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var checkForBlock = await _guildUserService.FetchGuildUserAsync(user.Id);
            if (checkForBlock is null)
            {
                await _guildUserService.CreateGuildUserAsync(user.Id, user.Name, user.Discriminator, false);
                await Context.AddConfirmationAsync();
            }

            if (checkForBlock.IsModmailBlocked)
            {
                await _guildUserService.UpdateGuildUserAsync(user.Id, null, null, false);
                await Context.AddConfirmationAsync();
            }
            else if (!checkForBlock.IsModmailBlocked)
            {
                throw new InvalidOperationException("The user provided is not currently blocked.");
            }
        }

        [Command("contact")]
        [RequireClaims(ClaimMapType.ModmailManage)]
        [Description("Creates a modmail thread manually with the user.")]
        public async Task ContactUserAsync(
            [Description("The user to contact.")] 
                IMember user,
            [Description("The message to be sent to the user upon the ticket being created.")] [Remainder]
                string message)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketAsync(user.Id);
            if (modmail is not null)
                throw new Exception(
                    $"There is already an ongoing thread with this user in <#{modmail.ModmailChannelId}>.");
            var modmailCategory = Context.Guild.GetChannel(DoraemonConfig.ModmailCategoryId) as ICategoryChannel;
            var textChannel =
                await Context.Guild.CreateTextChannelAsync(user.Tag,
                    x => x.CategoryId = modmailCategory.Id);
            var highestRole = Context.Author.GetRoles().Select(x => x.Value).OrderByDescending(x => x.Position).First().Name;
            var userEmbed = new LocalEmbed()
                .WithTitle($"You have been contacted by the Staff of {Context.Guild.Name}")
                .WithAuthor(Context.Author)
                .WithDescription(message)
                .WithColor(DColor.Green)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter($"{highestRole}");
            IUserMessage messageEmbed = null;
            try
            {
                messageEmbed = await user.SendMessageAsync(new LocalMessage().WithEmbeds(userEmbed));
            }
            catch
            {
                throw new Exception("The user provided has Direct Messages disabled, thus I was unable to contact them.");
            }

            var ID = DatabaseUtilities.ProduceId();
            await textChannel.SendMessageAsync(new LocalMessage().WithContent(
                $"Thread successfully started with {user.Tag}\nID: `{ID}`\nContacter: {Context.Author.Tag}"));
            await _modmailTicketService.CreateModmailTicketAsync(ID, user.Id,messageEmbed.ChannelId ,textChannel.Id);
            await Context.AddConfirmationAsync();
        }
    }
}