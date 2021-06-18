using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Modules
{
    [Name("Connectivity")]
    [Summary("Provides utilities for making sure that Doraemon is alive and healthy.")]
    public class ConnectivityModule : ModuleBase
    {
        public DiscordSocketClient _client;
        public ConnectivityModule(DiscordSocketClient client)
        {
            _client = client;
        }
        [Command("ping")]
        [Summary("Used for making sure Doraemon is healthy.")]
        public async Task PingAsync()
        {
            var dateTime = DateTime.Now - Context.User.CreatedAt;
            var embed = new EmbedBuilder()
                .WithTitle($"🏓 Pong!")
                .WithDescription($"I am up and healty, with a ping time between me and the Discord API being {_client.Latency}")
                .WithFooter($"I received the message within {dateTime.Milliseconds} milliseconds.")
                .WithColor(Color.Blue)
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}
