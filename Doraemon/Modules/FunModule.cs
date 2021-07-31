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
using Qmmands;

namespace Doraemon.Modules
{
    public class FunModule : DiscordGuildModuleBase
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly HttpClient _httpClient;
        private readonly InfractionService _infractionService;

        public FunModule(HttpClient httpClient, InfractionService infractionService)
        {
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