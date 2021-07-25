using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Services;
using Humanizer;
using Newtonsoft.Json;

namespace Doraemon.Modules
{
    [Name("Connectivity")]
    [Summary("Provides utilities for making sure that Doraemon is alive and healthy.")]
    public class ConnectivityModule : ModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClient _httpClient;

        public ConnectivityModule(DiscordSocketClient client, HttpClient httpClient)
        {
            _client = client;
            _httpClient = httpClient;
        }

        [Command("ping")]
        [Alias("test")]
        [Summary("Used for making sure Doraemon is healthy.")]
        public async Task PingAsync()
        {
            var dateTime = DateTime.Now - Context.User.CreatedAt;
            var embed = new EmbedBuilder()
                .WithTitle("🏓 Pong!")
                .WithDescription(
                    $"I am up and healty, with a ping time between me and the Discord API being {_client.Latency}")
                .WithFooter($"I received the message within {dateTime.Milliseconds} milliseconds.")
                .WithColor(Color.Blue)
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("api")]
        [Alias("dapi", "api status")]
        [Summary("Gets the uptime of the bot and checks the status of the Discord API.")]
        public async Task DisplayAPIStatusAsync()
        {
            var serializedResult = await _httpClient.GetStringAsync("https://discordstatus.com/api/v2/status.json");
            var result = JsonConvert.DeserializeObject<DiscordStatus>(serializedResult);
            var embed = new EmbedBuilder()
                .WithTitle("Discord API Current Status")
                .AddField("Current State", result.Status.Indicator, true)
                .AddField("Description", result.Status.Description, true)
                .AddField("Last Updated", result.Page.UpdatedAt.ToString("f"))
                .WithColor(Color.Blue)
                .WithCurrentTimestamp()
                .WithFooter("\"None\" means that the API is not feeling any stress, and is working as intended.")
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("uptime")]
        [Alias("howlong", "timeup")]
        [Summary("Gets the uptime of the bot.")]
        public async Task DisplayUptimeAsync()
        {
            var time = CommandHandler.stopwatch.Elapsed;

            await ReplyAsync($"{time.Humanize()}");
        }
    }

    public class Page
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string TimeZone { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Status
    {
        public string Indicator { get; set; }
        public string Description { get; set; }
    }

    public class DiscordStatus
    {
        public Page Page { get; set; }
        public Status Status { get; set; }
    }
}