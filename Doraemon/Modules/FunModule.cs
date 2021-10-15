using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Gateway.Default.Dispatcher;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data.Models;
using Doraemon.Services.Moderation;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Qmmands;
using Serilog;

namespace Doraemon.Modules
{
    public class FunModule : DoraemonGuildModuleBase
    {
        private readonly HttpClient _httpClient;
        public DoraemonConfiguration DoraemonConfig = new();
        public FunModule(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Command("dogfact")]
        [Description("Fetches a fact about man's best friend.")]
        public async Task<DiscordCommandResult> FetchDogfactAsync() // Fetch is a pun.
        {
            var url = "https://dog-api.kinduff.com/api/facts";
            var serializedResult = await _httpClient.GetStringAsync(url);
            var deserializedResult = JsonConvert.DeserializeObject<DogFact>(serializedResult);
            if (deserializedResult.Facts.Count == 0 || !deserializedResult.Success)
                throw new Exception("There was an error fetching a fact from the API.");
            return Response($"Dogfact: {deserializedResult.Facts[0]}");
        }

        [Command("meme")]
        [Description("Fetches a meme from the r/memes subreddit.")]
        public async Task<DiscordCommandResult> FetchMemeAsync()
        {
            var redditUrl = "https://www.reddit.com/r/memes/random.json?limit=1";
            var jsonMeme = await _httpClient.GetStringAsync(redditUrl);
            var jsonMemeArray = JArray.Parse(jsonMeme);
            var redditPost = JObject.Parse(jsonMemeArray[0]["data"]["children"][0]["data"].ToString());
            Boolean.TryParse(redditPost["over_18"].ToString(), out var isNsfw);
            if (isNsfw)
            {
                Log.Logger.Information($"{Context.Author.Tag} attempted to run the meme command, but an NSFW meme was retrieved.");
                return Response("The post retrieved was marked NSFW, please run the command again.");
            }
            var embed = new LocalEmbed()
                .WithTitle(redditPost["title"].ToString())
                .WithImageUrl(redditPost["url"].ToString())
                .WithUrl($"https://www.reddit.com{redditPost["permalink"]}")
                .WithColor(DColor.Red)
                .WithFooter($"⬆️{redditPost["ups"]}");
            return Response(embed);
        }

        [Command("pocketsand")]
        [Description("Punishes a user by pocketsand.")]
        public DiscordCommandResult PocketsandUserAsync(IMember user)
        {
            return Response($"{Context.Author.Mention} pocketsands {user.Mention}, Shi Shi Sha!!!!!\n https://tenor.com/view/king-of-the-hill-dale-gribble-pocket-sand-pocket-sand-gif-3699662");
        }

        [Command("quote")]
        [Description("Quotes a message.")]
        public async Task<DiscordCommandResult> QuoteAsync(
            [Description("THe ID of the message.")]
                Snowflake messageId)
        {
            IUserMessage message = null;
            foreach (var channel in Context.Guild.GetChannels().Where(x => x.Value.Type == ChannelType.Text).Select(x => x.Value).ToList())
            {
                message = await Bot.FetchMessageAsync(channel.Id, messageId) as IUserMessage;
                if (message == null)
                {
                    continue;
                }
                
                break;
            }

            if (message == null)
            {
                throw new Exception("The message ID provided couldn't be found.");
            }
            var messageChannel = Context.Guild.GetChannel(message.ChannelId);

            if (message.Embeds.Any())
            {
                var embedEmbed = GetEmbedForQuote(message);
                embedEmbed.AddField("Quoted By", $"{Context.Author.Mention} from [#{messageChannel.Name}(click here)](https://www.discord.com/channels/{messageChannel.GuildId}/{messageChannel.Id}/{message.Id})");
                return Response(embedEmbed);
            }

            var embed = new LocalEmbed()
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .AddField("Quoted By", $"{Context.Author.Mention} from [#{messageChannel.Name}(click here)](https://www.discord.com/channels/{messageChannel.GuildId}/{messageChannel.Id}/{message.Id})")
                .WithColor(DColor.Green)
                .WithTimestamp(DateTimeOffset.UtcNow);
            return Response(embed);
        }

        [Command("quote")]
        [Description("Quotes a message.")]
        public async Task<DiscordCommandResult> QuoteAsync(
            [Description("The channel that the message originated from.")]
                ITextChannel channel,
            [Description("The ID of the message.")]
                Snowflake messageId)
        {
            var message = await channel.FetchMessageAsync(messageId) as IUserMessage;
            if(message == null)
            {
                throw new Exception($"The message ID provided was not found in {Mention.Channel(channel)}(maybe try using the `{DoraemonConfig.Prefix}quote <messageId>` overload?)");
            }

            var messageChannel = Context.Guild.GetChannel(channel.Id);
            if (message.Embeds.Any())
            {
                var embedEmbed = GetEmbedForQuote(message);
                embedEmbed.AddField("Quoted By", $"{Context.Author.Mention} from [#{messageChannel.Name}(click here)](https://www.discord.com/channels/{messageChannel.GuildId}/{messageChannel.Id}/{message.Id})");
                return Response(embedEmbed);
            }

            var embed = new LocalEmbed()
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .AddField("Quoted By", $"{Context.Author.Mention} from [#{messageChannel.Name}(click here)](https://www.discord.com/channels/{messageChannel.GuildId}/{messageChannel.Id}/{message.Id})")
                .WithColor(DColor.Green)
                .WithTimestamp(DateTimeOffset.UtcNow);
            return Response(embed);
        }

        private LocalEmbed GetEmbedForQuote(IUserMessage message)
        {
            var embed = message.Embeds.ElementAt(0);
            return embed.ToLocalEmbed();
        }
    }

    public class DogFact
    {
        public List<string> Facts { get; set; }
        public bool Success { get; set; }
    }
}