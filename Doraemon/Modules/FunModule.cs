using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Moderation;
using Humanizer;
using Newtonsoft.Json;

namespace Doraemon.Modules
{
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly HttpClient _httpClient;
        private readonly InfractionService _infractionService;

        public FunModule(HttpClient httpClient, InfractionService infractionService)
        {
            _httpClient = httpClient;
            _infractionService = infractionService;
        }

        [Command("dogfact")]
        [Alias("pup fact")]
        [Summary("Fetches a fact about man's best friend.")]
        public async Task FetchDogfactAsync() // Fetch is a pun.
        {
            var url = "https://dog-api.kinduff.com/api/facts";
            var serializedResult = await _httpClient.GetStringAsync(url);
            var deserializedResult = JsonConvert.DeserializeObject<DogFact>(serializedResult);
            if (deserializedResult.Facts.Count == 0 || !deserializedResult.Success)
                throw new Exception("There was an error fetching a fact from the API.");
            await ReplyAsync($"Dogfact: {deserializedResult.Facts[0]}");
        }

        [Command("pocketsand")]
        [Alias("punish")]
        [Summary("Punishes a user by pocketsand.")]
        public async Task PocketsandUserAsync(SocketGuildUser user)
        {
            await ReplyAsync(
                $"{Context.User.Mention} pocketsands {user.Mention}, Shi Shi Sha!!!!!\n https://tenor.com/view/king-of-the-hill-dale-gribble-pocket-sand-pocket-sand-gif-3699662");
        }

        [Command("selfmute")]
        [RequireContext(ContextType.Guild)]
        [Summary("Allows you to mute yourself.")]
        public async Task SelfmuteAsync(
            [Summary("The duration for the mute.")]
                TimeSpan duration)
        {
            await _infractionService.CreateInfractionAsync(Context.User.Id, Context.User.Id, Context.Guild.Id,
                InfractionType.Mute, "Self-Mute", false, duration);
            var muteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            await (Context.User as SocketGuildUser).AddRoleAsync(muteRole);
            await Context.AddConfirmationAsync();
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            try
            {
                await dmChannel.SendMessageAsync(
                    $"You have self-muted yourself for {duration.Humanize()}. You will be automatically unmuted after the given duration passes. If you feel this was a mistake, please contact Staff.");
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                await ReplyAsync(
                    $"{Context.User.Mention}, you have self-muted yourself for {duration.Humanize()}. You will be automatically unmuted after the given duration passes. If you feel this was a mistake, please contact Staff.");
            }
        }
    }

    public class DogFact
    {
        public List<string> Facts { get; set; }
        public bool Success { get; set; }
    }
}