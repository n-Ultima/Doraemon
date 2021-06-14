using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Events.MessageReceivedHandlers;
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

namespace Doraemon.Modules
{
    [Name("Modmail")]
    [Summary("Contains all the commands used for handling modmail tickets.")]
    [RequireStaff]
    public class ModmailModule : ModuleBase<SocketCommandContext>
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModmailModule(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        [Command("reply")]
        [Summary("Replies to a current modmail thread.")]
        public async Task ReplyTicketAsync(
            [Summary("The ticket ID to reply to.")]
                string ID,
            [Summary("The response")]
                [Remainder] string response)
        {
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
                .WithAuthor(Context.User.GetFullUsername(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                .WithColor(Color.Green)
                .WithDescription(response)
                .WithFooter($"{highestRole} • {Context.Message.CreatedAt.ToString("f")}")
                .Build();
            await dmChannel.SendMessageAsync(embed: embed);
            await Context.AddConfirmationAsync();
        }
        [Command("close")]
        [Summary("Closes the modmail thread that the command is run inside of.")]
        public async Task CloseTicketAsync()
        {
            var modmail = await _doraemonContext
                .Set<ModmailTicket>()
                .Where(x => x.ModmailChannel == Context.Channel.Id)
                .SingleOrDefaultAsync();
            if(modmail is null)
            {
                throw new NullReferenceException("This channel is not a modmail thread.");
            }
            var channel = Context.Channel as ITextChannel;
            await channel.DeleteAsync();
            var embed = new EmbedBuilder()
                .WithTitle("Thread Closed")
                .WithColor(Color.Red)
                .WithDescription($"{Context.User.Mention} has closed this Modmail thread.")
                .WithFooter(iconUrl: Context.Guild.IconUrl, text: $"Replying will create a new thread  • {Context.Message.CreatedAt.ToString("d")}")
                .Build();
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed);
            _doraemonContext.ModmailTickets.Remove(modmail);
            await _doraemonContext.SaveChangesAsync();
        }
        [Command("contact")]
        [Summary("Creates a modmail thread manually with the user.")]
        public async Task ContactUserAsync(
            [Summary("The user to contact.")]
                SocketGuildUser user,
            [Summary("The message to be sent to the user upon the ticket being created.")]
                [Remainder] string message)
        {
            var modmail = await _doraemonContext
                .Set<ModmailTicket>()
                .Where(x => x.UserId == user.Id)
                .SingleOrDefaultAsync();
            if(modmail is not null)
            {
                throw new Exception($"There is already an ongoing thread with this user in <#{modmail.ModmailChannel}>.");
            }
            var modmailCategory = Context.Guild.GetCategoryChannel(DoraemonConfig.ModmailCategory);
            var textChannel = await Context.Guild.CreateTextChannelAsync(user.GetFullUsername(), x => x.CategoryId = modmailCategory.Id);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            var highestRole = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
            var userEmbed = new EmbedBuilder()
                .WithTitle($"You have been contacted by the Staff of {Context.Guild.Name}")
                .WithAuthor(Context.User.GetFullUsername(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                .WithDescription(message)
                .WithColor(Color.Green)
                .WithFooter($"{highestRole} • {Context.Message.CreatedAt.ToString("f")}")
                .Build();
            try
            {
                await dmChannel.SendMessageAsync(embed: userEmbed);
            }
            catch(HttpException ex) when (ex.DiscordCode == 50007)
            {
                throw new Exception("The user provided has Direct Messages disabled, thus I was unable to contact them.");
            }
            var ID = await DatabaseUtilities.ProduceIdAsync();
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
