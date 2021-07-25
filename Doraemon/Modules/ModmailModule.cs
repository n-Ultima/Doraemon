using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.CommandHelp;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Services.Moderation;

namespace Doraemon.Modules
{
    [Name("Modmail")]
    [Summary("Contains all the commands used for handling modmail tickets.")]
    [HelpTags("modmail", "support", "tickets")]
    public class ModmailModule : ModuleBase<SocketCommandContext>
    {
        private readonly GuildUserService _guildUserService;
        private readonly ModmailTicketService _modmailTicketService;
        private readonly AuthorizationService _authorizationService;
        private readonly DiscordSocketClient _client;

        public ModmailModule(DiscordSocketClient client, AuthorizationService authorizationService,
            ModmailTicketService modmailTicketService, GuildUserService guildUserService)
        {
            _guildUserService = guildUserService;
            _client = client;
            _authorizationService = authorizationService;
            _modmailTicketService = modmailTicketService;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        [Command("reply")]
        [Alias("respond")]
        [Summary("Replies to a current modmail thread.")]
        public async Task ReplyTicketAsync(
            [Summary("The ticket ID to reply to.")]
                string ID,
            [Summary("The response")] [Remainder] 
                string response)
        {
            await _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketAsync(ID);
            if (modmail is null) throw new NullReferenceException("The ID provided is invalid.");
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await _client.GetDMChannelAsync(modmail.DmChannelId);
            if (dmChannel is null) dmChannel = await user.GetOrCreateDMChannelAsync();
            var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
            if (highestRole is null) highestRole = "@everyone";
            var embed = new EmbedBuilder()
                .WithAuthor(Context.User.GetFullUsername(), Context.User.GetDefiniteAvatarUrl())
                .WithColor(Color.Green)
                .WithDescription(response)
                .WithCurrentTimestamp()
                .WithFooter($"{highestRole}")
                .Build();
            await dmChannel.SendMessageAsync(embed: embed);
            await Context.AddConfirmationAsync();
        }

        [Command("close")]
        [Alias("delete")]
        [Summary("Closes the modmail thread that the command is run inside of.")]
        public async Task CloseTicketAsync()
        {
            await _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(Context.Channel.Id);
            if (modmail is null) throw new NullReferenceException("This channel is not a modmail thread.");
            var id = modmail.Id;
            var channel = Context.Channel as ITextChannel;
            await channel.DeleteAsync();
            var embed = new EmbedBuilder()
                .WithTitle("Thread Closed")
                .WithColor(Color.Red)
                .WithDescription($"{Context.User.Mention} has closed this Modmail thread.")
                .WithCurrentTimestamp()
                .WithFooter(iconUrl: Context.Guild.IconUrl, text: "Replying will create a new thread")
                .Build();
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed);

            var modmailLogChannel = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId);

            
            var path = Path.Combine(Environment.CurrentDirectory, "modmailLogs.txt");
            using (var file = File.Create($"{path}", 1024))
            {
                foreach (var x in await _modmailTicketService.FetchModmailMessagesAsync(modmail.Id))
                {
                    var info = new UTF8Encoding(true).GetBytes(x.Content);
                    file.Write(info, 0, info.Length);
                }

                file.Close();

                await modmailLogChannel.SendFileAsync(path, "Modmail Log");


                File.Delete(path);
            }
            await _modmailTicketService.DeleteModmailTicketAsync(id);

        }

        [Command("block")]
        [Summary("Blocks a user from creating modmail threads.")]
        public async Task BlockUserAsync(
            [Summary("The user to block.")] 
                SocketGuildUser user,
            [Summary("The reason for the block.")] [Remainder]
                string reason)
        {
            await _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var checkForBlock = await _guildUserService.FetchGuildUserAsync(user.Id);
            if (checkForBlock is null)
            {
                await _guildUserService.CreateGuildUserAsync(user.Id, user.Username, user.Discriminator, true);
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
        [Summary("Unblocks a user from the modmail system.")]
        public async Task UnblockUserAsync(
            [Summary("The user to unblock.")] 
                SocketGuildUser user,
            [Summary("The reason for the unblock.")] [Remainder]
                string reason)
        {
            await _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var checkForBlock = await _guildUserService.FetchGuildUserAsync(user.Id);
            if (checkForBlock is null)
            {
                await _guildUserService.CreateGuildUserAsync(user.Id, user.Username, user.Discriminator, false);
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
        [Summary("Creates a modmail thread manually with the user.")]
        public async Task ContactUserAsync(
            [Summary("The user to contact.")] 
                SocketGuildUser user,
            [Summary("The message to be sent to the user upon the ticket being created.")] [Remainder]
                string message)
        {
            await _authorizationService.RequireClaims(ClaimMapType.ModmailManage);
            var modmail = await _modmailTicketService.FetchModmailTicketAsync(user.Id);
            if (modmail is not null)
                throw new Exception(
                    $"There is already an ongoing thread with this user in <#{modmail.ModmailChannelId}>.");
            var modmailCategory = Context.Guild.GetCategoryChannel(DoraemonConfig.ModmailCategoryId);
            var textChannel =
                await Context.Guild.CreateTextChannelAsync(user.GetFullUsername(),
                    x => x.CategoryId = modmailCategory.Id);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
            var userEmbed = new EmbedBuilder()
                .WithTitle($"You have been contacted by the Staff of {Context.Guild.Name}")
                .WithAuthor(Context.User.GetFullUsername(), Context.User.GetDefiniteAvatarUrl())
                .WithDescription(message)
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"{highestRole}")
                .Build();
            try
            {
                await dmChannel.SendMessageAsync(embed: userEmbed);
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                throw new Exception(
                    "The user provided has Direct Messages disabled, thus I was unable to contact them.");
            }

            var ID = DatabaseUtilities.ProduceId();
            await textChannel.SendMessageAsync(
                $"Thread successfully started with {user.GetFullUsername()}\nID: `{ID}`\nContacter: {Context.User.GetFullUsername()}");
            await _modmailTicketService.CreateModmailTicketAsync(ID, user.Id, dmChannel.Id, textChannel.Id);
            await Context.AddConfirmationAsync();
        }
    }
}