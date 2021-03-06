using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Newtonsoft.Json;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Connectivity")]
    [Description("Provides utilities for making sure that Doraemon is alive and healthy.")]
    public class ConnectivityModule : DoraemonGuildModuleBase
    {
        private readonly HttpClient _httpClient;

        public ConnectivityModule(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Command("ping")]
        [Description("Pings")]
        public DiscordCommandResult PingAsync()
        {
            var dateTime = DateTimeOffset.UtcNow - Context.Message.CreatedAt();
            var heartbeatLatency = Context.Message.GetGatewayClient().ApiClient.Heartbeater.Latency;
            var builder = new StringBuilder();
            if (!heartbeatLatency.HasValue)
            {
                builder.Append($"🏓 Pong!\nShard Latency: {Context.Bot.GetShard(Context.GuildId).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
            }
            else
            {
                builder.Append($"🏓 Pong!\nDirect API Latency: {heartbeatLatency.Value.Milliseconds} ms\nShard Latency: {Context.Bot.GetShard(Context.GuildId).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
            }

            return Response(builder.ToString());
        }
        [Command("api", "dapi")]
        [Description("Gets the uptime of the bot and checks the status of the Discord API.")]
        public async Task<DiscordCommandResult> DisplayAPIStatusAsync()
        {
            var serializedResult = await _httpClient.GetStringAsync("https://discordstatus.com/api/v2/status.json");
            var result = JsonConvert.DeserializeObject<DiscordStatus>(serializedResult);
            var embed = new LocalEmbed()
                .WithTitle("Discord API Current Status")
                .AddField("Current State", result.Status.Indicator, true)
                .AddField("Description", result.Status.Description, true)
                .AddField("Last Updated", result.Page.UpdatedAt.ToString("f"))
                .WithColor(DColor.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter("\"None\" means that the API is not feeling any stress, and is working as intended.");
            return Response(new LocalMessage().WithEmbeds(embed));
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