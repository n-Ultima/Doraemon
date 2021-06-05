using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Common;
using Doraemon.Common.Utilities;

namespace Doraemon.Data.Events.MessageReceivedHandlers
{
    public class ModmailHandler
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModmailHandler(DiscordSocketClient client, DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        public async Task ModmailAsync(SocketMessage arg)
        {
            if (arg.Author.IsBot) return;
            if((arg.Channel.GetType()) == typeof (SocketDMChannel))
            {
                var dmModmail = await _doraemonContext
                    .Set<ModmailTicket>()
                    .Where(x => x.DmChannel == arg.Channel.Id)
                    .Where(x => x.UserId == arg.Author.Id)
                    .SingleOrDefaultAsync();
                var modMailGuild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var modMailCategory = modMailGuild.GetCategoryChannel(DoraemonConfig.ModmailCategory);
                if (dmModmail is null)
                {
                    var ID = await DatabaseUtilities.ProduceIdAsync();
                    await arg.Channel.SendMessageAsync("Thank you for contacting Modmail! Staff will reply as soon as possible.");
                    var textChannel = await modMailGuild.CreateTextChannelAsync(await arg.Author.GetFullUsername(), x => x.CategoryId = modMailCategory.Id);

                    var firstMessageEmbed = new EmbedBuilder()
                    .WithAuthor(await arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Gold)
                    .WithDescription(arg.Content)
                    .WithFooter($"Message ID: {arg.Id} • {arg.CreatedAt.ToString("f")}\nTicket ID: {ID}")
                    .Build();
                    await textChannel.SendMessageAsync(embed: firstMessageEmbed);
                    _doraemonContext.ModmailTickets.Add(new ModmailTicket { Id = ID, DmChannel = arg.Channel.Id, ModmailChannel = textChannel.Id, UserId = arg.Author.Id });
                    await _doraemonContext.SaveChangesAsync();
                    await arg.AddConfirmationAsync();
                    return;
                }
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var channelToSend = guild.GetTextChannel(dmModmail.ModmailChannel);
                var embed = new EmbedBuilder()
                    .WithAuthor(await arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Gold)
                    .WithDescription(arg.Content)
                    .WithFooter($"Message ID: {arg.Id} • {arg.CreatedAt.ToString("f")}")
                    .Build();
                await channelToSend.SendMessageAsync(embed: embed);
            }
            else
            {
                if (arg.Content.Contains("!close"))
                {
                    return;
                }
                var modmail = await _doraemonContext
                    .Set<ModmailTicket>()
                    .Where(x => x.ModmailChannel == arg.Channel.Id)
                    .SingleOrDefaultAsync();
                var user = _client.GetUser(modmail.UserId);
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                var highestRole = (arg.Author as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
                var embed = new EmbedBuilder()
                    .WithAuthor(await arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Green)
                    .WithDescription(arg.Content)
                    .WithFooter($"{highestRole} • {arg.CreatedAt.ToString("f")}")
                    .Build();
                await dmChannel.SendMessageAsync(embed: embed);
                await arg.AddConfirmationAsync();
            }
        }
    }
}
