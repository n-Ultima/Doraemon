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

namespace Doraemon.Modules
{
    [Name("Modmail")]
    [Summary("Contains all the commands used for handling modmail tickets.")]
    [RequireStaff]
    public class ModmailModule : ModuleBase<SocketCommandContext>
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public ModmailModule(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
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
            var f = new EmbedFooterBuilder()
            {
                IconUrl = Context.Guild.IconUrl,
                Text = "Replying will create a new thread  • {Context.Message.CreatedAt.ToString("d")}",
            };
            var embed = new EmbedBuilder()
                .WithTitle("Thread Closed")
                .WithColor(Color.Red)
                .WithDescription($"{Context.User.Mention} has closed this Modmail thread.")
                .WithFooter(f)
                .Build();
            var user = _client.GetUser(modmail.UserId);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed);
            _doraemonContext.ModmailTickets.Remove(modmail);
            await _doraemonContext.SaveChangesAsync();
        }
    }
}
