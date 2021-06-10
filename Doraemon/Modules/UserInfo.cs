using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common.Utilities;
using Doraemon.Common.Extensions;

namespace Doraemon.Modules
{
    public class UserInfo : ModuleBase<SocketCommandContext>
    {
        public DiscordSocketClient _client;
        public UserInfo(DiscordSocketClient client)
        {
            _client = client;
        }
        [Command("info")]
        [Summary("Displays info about the user, or the author if none is provided.")]
        public async Task DisplayUserInfoAsync(
            [Summary("The user to query for information.")]
                SocketGuildUser user = null)
        {
            if (user == null)
            {
                user = Context.User as SocketGuildUser;
            }
            var roles = (user as SocketGuildUser).Roles
                .Where(x => x.Id != Context.Guild.EveryoneRole.Id && x.Color != Color.Default)
                .OrderByDescending(x => x.Position)
                .ThenByDescending(x => x.IsHoisted);
            var embed = new EmbedBuilder()
                .WithAuthor(await user.GetFullUsername(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .AddField("Creation", user.CreatedAt.ToString("d"), true)
                .AddField("Username", user.Username, true)
                .AddField("Discriminator", user.Discriminator, true)
                .AddField("Hierarchy", user.Hierarchy, true)
                .AddField("Roles", roles.Humanize())
                .WithColor(Color.DarkBlue)
                .Build();
            await ReplyAsync(embed: embed);
                
                
        }
        [Command("avatar")]
        [Summary("Gets a user's avatat.")]
        public async Task GetAvatarAsync(ulong userId)
        {
            var user = await _client.Rest.GetUserAsync(userId);
            var e = new EmbedBuilder()
                .WithImageUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithAuthor(user.Username + "#" + user.Discriminator, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .Build();
            await ReplyAsync(embed: e);
        }
        [Command("status")]
        [Summary("The the status of the bot.")]
        public async Task SetStatusAsync(
            [Summary("The status for the bot to be set to.")]
                [Remainder] string status)
        {
            await _client.SetGameAsync(status, type: ActivityType.Playing);
        }

    }
}
