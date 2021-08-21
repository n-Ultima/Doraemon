using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Doraemon.Common.Extensions;
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
    }

    public class DogFact
    {
        public List<string> Facts { get; set; }
        public bool Success { get; set; }
    }
}