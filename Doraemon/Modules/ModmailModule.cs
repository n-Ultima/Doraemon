using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Data;
using Discord.Commands;
using Discord;
using Doraemon.Data.Models;
using Doraemon.Common.Attributes;
using Discord.WebSocket;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Common;
using Doraemon.Common.Utilities;
using Discord.Net;
using Doraemon.Services.Core;
using Doraemon.Data.Models.Core;
using System.IO;

namespace Doraemon.Modules
{
    [Name("Modmail")]
    [Summary("Contains all the commands used for handling modmail tickets.")]
    public class ModmailModule : ModuleBase<SocketCommandContext>
    {
        public DoraemonContext _doraemonContext;
        public AuthorizationService _authorizationService;
        public DiscordSocketClient _client;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModmailModule(DoraemonContext doraemonContext, DiscordSocketClient client, AuthorizationService authorizationService)
        {
            _doraemonContext = doraemonContext;
            _client = client;
            _authorizationService = authorizationService;
        }
        [Command("reply")]
        [Summary("Replies to a current modmail thread.")]
        public async Task ReplyTicketAsync(
            [Summary("The ticket ID to reply to.")]
                string ID,
            [Summary("The response")]
                [Remainder] string response)
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.ModmailManage);
            var modmail = await _doraemonContext
                .Set<ModmailTicket>()
                .Where(x => x.Id == ID)
                .SingleOrDefaultAsync();
            if(modmail is null)
            {
                throw new NullReferenceException("The ID provided is invalid.");
            }
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await _client.GetDMChannelAsync(modmail.DmChannel);
            if(dmChannel is null)
            {
                dmChannel = await user.GetOrCreateDMChannelAsync();
            }
            var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
            if (highestRole is null)
            {
                highestRole = "@everyone";
            }
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
        [Summary("Closes the modmail thread that the command is run inside of.")]
        public async Task CloseTicketAsync()
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.ModmailManage);
            var modmail = await _doraemonContext
                .Set<ModmailTicket>()
                .Where(x => x.ModmailChannel == Context.Channel.Id)
                .SingleOrDefaultAsync();
            if(modmail is null)
            {
                throw new NullReferenceException("This channel is not a modmail thread.");
            }
            var id = modmail.Id;
            var channel = Context.Channel as ITextChannel;
            await channel.DeleteAsync();
            var embed = new EmbedBuilder()
                .WithTitle("Thread Closed")
                .WithColor(Color.Red)
                .WithDescription($"{Context.User.Mention} has closed this Modmail thread.")
                .WithCurrentTimestamp()
                .WithFooter(iconUrl: Context.Guild.IconUrl, text: $"Replying will create a new thread")
                .Build();
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed);
            _doraemonContext.ModmailTickets.Remove(modmail);
            await _doraemonContext.SaveChangesAsync();

            var modmailLogChannel = Context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId);

            if(ModmailHandler.stringBuilder.ToString() == null)
            {
                return;
            }
            ModmailHandler.stringBuilder.AppendLine($"Ticket Closed By **(Staff){Context.User.GetFullUsername()}**");
            ModmailHandler.stringBuilder.AppendLine();
            ModmailHandler.stringBuilder.AppendLine();
            string path = "modmailLogs.txt";
            using (FileStream file = File.Create($"{path}", 1024))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(ModmailHandler.stringBuilder.ToString());

                file.Write(info, 0, info.Length);
                file.Close();

                await modmailLogChannel.SendFileAsync(path, "Modmail Log");

                ModmailHandler.stringBuilder.Clear();

                File.Delete(path);
            }
        }
        [Command("block")]
        [Summary("Blocks a user from creating modmail threads.")]
        public async Task BlockUserAsync(
            [Summary("The user to block.")]
                SocketGuildUser user,
            [Summary("The reason for the block.")]    
                [Remainder] string reason)
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.ModmailManage);
            var checkForBlock = await _doraemonContext.GuildUsers
                .Where(x => x.Id == user.Id)
                .SingleOrDefaultAsync();
            if(checkForBlock is null)
            {
                _doraemonContext.GuildUsers.Add(new Data.Models.Core.GuildUser
                {
                    Id = user.Id,
                    Discriminator = user.Discriminator,
                    IsModmailBlocked = true,
                    Username = user.Username,
                });
                await _doraemonContext.SaveChangesAsync();
                await Context.AddConfirmationAsync();
            }
            else
            {
                if (checkForBlock.IsModmailBlocked)
                {
                    throw new InvalidOperationException($"The user provided is already blocked.");
                }
                checkForBlock.IsModmailBlocked = true;
                await _doraemonContext.SaveChangesAsync();
                await Context.AddConfirmationAsync();
            }
        }
        [Command("unblock")]
        [Summary("Unblocks a user from the modmail system.")]
        public async Task UnblockUserAsync(
            [Summary("The user to unblock.")]
                SocketGuildUser user,
            [Summary("The reason for the unblock.")]
                [Remainder] string reason)
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.ModmailManage);
            var checkForBlock = await _doraemonContext.GuildUsers
                .Where(x => x.Id == user.Id)
                .SingleOrDefaultAsync();
            if(checkForBlock is null)
            {
                _doraemonContext.GuildUsers.Add(new GuildUser
                {
                    Id = user.Id,
                    Discriminator = user.Discriminator,
                    IsModmailBlocked = false,
                    Username = user.Username,
                });
                await _doraemonContext.SaveChangesAsync();
                await Context.AddConfirmationAsync();
            }
            if (checkForBlock.IsModmailBlocked)
            {
                checkForBlock.IsModmailBlocked = false;
                await _doraemonContext.SaveChangesAsync();
                await Context.AddConfirmationAsync();
            }
            else if(!checkForBlock.IsModmailBlocked)
            {
                throw new InvalidOperationException($"The user provided is not currently blocked.");
            }
        }
        [Command("contact")]
        [Summary("Creates a modmail thread manually with the user.")]
        public async Task ContactUserAsync(
            [Summary("The user to contact.")]
                SocketGuildUser user,
            [Summary("The message to be sent to the user upon the ticket being created.")]
                [Remainder] string message)
        {
            await _authorizationService.RequireClaims(Context.User.Id, ClaimMapType.ModmailManage);
            var modmail = await _doraemonContext
                .Set<ModmailTicket>()
                .Where(x => x.UserId == user.Id)
                .SingleOrDefaultAsync();
            if(modmail is not null)
            {
                throw new Exception($"There is already an ongoing thread with this user in <#{modmail.ModmailChannel}>.");
            }
            var modmailCategory = Context.Guild.GetCategoryChannel(DoraemonConfig.ModmailCategoryId);
            var textChannel = await Context.Guild.CreateTextChannelAsync(user.GetFullUsername(), x => x.CategoryId = modmailCategory.Id);
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
            catch(HttpException ex) when (ex.DiscordCode == 50007)
            {
                throw new Exception("The user provided has Direct Messages disabled, thus I was unable to contact them.");
            }
            var ID = DatabaseUtilities.ProduceId();
            await textChannel.SendMessageAsync($"Thread successfully started with {user.GetFullUsername()}\nID: `{ID}`\nContacter: {Context.User.GetFullUsername()}");
            _doraemonContext.ModmailTickets.Add(new ModmailTicket
            {
                DmChannel = dmChannel.Id,
                ModmailChannel = textChannel.Id,
                Id = ID,
                UserId = user.Id
            });
            await _doraemonContext.SaveChangesAsync();
            await Context.AddConfirmationAsync();
        }
    }
}
