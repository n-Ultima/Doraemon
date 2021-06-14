using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Data;
using Newtonsoft.Json;


namespace Doraemon.Modules
{
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        public HttpClient _httpClient;
        public FunModule(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        [Command("dogfact")]
        [Summary("Fetches a fact about man's best friend.")]
        public async Task FetchDogfactAsync() // Fetch is a pun.
        {
            var url = "https://dog-api.kinduff.com/api/facts";
            var serializedResult = await _httpClient.GetStringAsync(url);
            var deserializedResult = JsonConvert.DeserializeObject<DogFact>(serializedResult);
            if (deserializedResult.Facts.Count == 0 || !deserializedResult.Success)
            {
                throw new Exception("There was an error fetching a fact from the API.");
            }
            await ReplyAsync($"Dogfact: {deserializedResult.Facts[0]}");
        }
        [Command("ping")]
        [Summary("The classic, ping ~ pong, with a twist.")]
        public async Task PingPongAsync()
        {
            await ReplyAsync($"Pong! I am online and functional.");
        }
    }
    public class DogFact
    {
        public List<string> Facts { get; set; }
        public bool Success { get; set; }
    }
}
