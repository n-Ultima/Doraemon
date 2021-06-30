using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Services.Events;

namespace Doraemon.Modules
{
    [Name("Text")]
    [Summary("Provides utilities for text messages.")]
    public class TextModule : ModuleBase<SocketCommandContext>
    {
        [Command("purge")]
        [Alias("clean")]
        [Summary("Mass-deletes messages from the channel ran-in.")]
        public async Task PurgeChannelAsync(
               [Summary("The number of messages to purge")]
                int amount)
        {
            if (!(Context.Channel is IGuildChannel channel))
            {
                throw new InvalidOperationException($"The channel that the command is ran in must be a guild channel.");
            }
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0)
            {
                return;
            }
            var messages = await Context.Channel.GetMessagesAsync(clampedCount).FlattenAsync();
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
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
            {
                throw new InvalidOperationException($"The channel that the command is ran in must be a guild channel.");
            }
            var channel = Context.Channel as ITextChannel;
            var clampedCount = Math.Clamp(amount, 0, 100);
            if (clampedCount == 0)
            {
                return;
            }
            var messages = (await channel.GetMessagesAsync(100).FlattenAsync()).Where(x => x.Author.Id == user.Id)
                .Take(clampedCount);
            await channel.DeleteMessagesAsync(messages);
        }
        [Command("snipe")]
        [Summary("Snipes a deleted message.")]
        public async Task SnipeDeletedMessageAsync(
            [Summary("The channel to snipe, defaults to the current channel.")]
                ITextChannel channel = null)
        {
            if (channel == null)
            {
                var deletedMessage = GuildEvents.DeletedMessages
                   .Find(x => x.ChannelId == Context.Channel.Id);

                if (deletedMessage == null)
                    await ReplyAsync("Nothing has been deleted yet!");
                else
                {
                    SocketGuildUser user = Context.Guild.GetUser(deletedMessage.UserId);

                    var embed = new EmbedBuilder()
                        .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
                        .WithDescription(deletedMessage.Content.ToString())
                        .WithTimestamp(deletedMessage.Time)
                        .WithColor(new Color(235, 0, 0))
                        .Build();

                    await ReplyAsync(embed: embed);

                }
                return;
            }

            //calling other channel's message
            var deletedMessage1 = GuildEvents.DeletedMessages
                               .Find(x => x.ChannelId == channel.Id);

            if (deletedMessage1 == null)
                await ReplyAsync("Nothing has been deleted yet!");
            else
            {
                SocketGuildUser user = Context.Guild.GetUser(deletedMessage1.UserId);

                var embed = new EmbedBuilder()
                    .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
                    .WithDescription(deletedMessage1.Content.ToString())
                    .WithTimestamp(deletedMessage1.Time)
                    .WithColor(new Color(235, 0, 0))
                    .Build();

                await ReplyAsync(embed: embed);
            }
        }
    }
}
