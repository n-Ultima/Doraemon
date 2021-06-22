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
                .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
                .AddField("Creation", user.CreatedAt.ToString("d"), true)
                .AddField("Joined Server", user.JoinedAt.Value.ToString("f"))
                .AddField("Username", user.Username, true)
                .AddField("Discriminator", user.Discriminator, true)
                .AddField("Hierarchy", user.Hierarchy, true)
                .AddField("PingRoles", roles.Humanize())
                .WithColor(Color.DarkBlue)
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("avatar")]
        [Summary("Gets a user's avatar.")]
        public async Task GetAvatarAsync(
            [Summary("The user whose avatar to be displayed.")]
                SocketGuildUser user)
        {
            var avatar = user.GetAvatarUrl(size: 2048) ?? user.GetDefaultAvatarUrl();
            var embed = new EmbedBuilder()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.GetFullUsername()}")
                .Build();
            await ReplyAsync(embed: embed);
        }
        [Command("avatar")]
        [Priority(10)]
        [Summary("Gets a user's avatar.")]
        public async Task GetAvatarAsync(
            [Summary("The ID of the user whose avatar to display.")]
                ulong userId)
        {
            var user = await _client.Rest.GetUserAsync(userId);
            var avatar = user.GetAvatarUrl(ImageFormat.Auto, 2048) ?? user.GetDefaultAvatarUrl();
            var e = new EmbedBuilder()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.GetFullUsername()}")
                .Build();
            await ReplyAsync(embed: e);
        }

    }
}
