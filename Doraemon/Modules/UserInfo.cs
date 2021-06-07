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
                ulong id = default)
        {
            var a = Context.Guild.GetUser(id);
            if(a is null)
            {
                var User = await _client.Rest.GetUserAsync(id);
                var buider = new StringBuilder()
                    .AppendLine("**\u276f User Information**")
                    .AppendLine($"ID: {User.Id}")
                    .AppendLine($"Profile: <@{User.Id}>")
                    .AppendLine($"Created: {User.CreatedAt.ToString("f")}");
                var e = new EmbedBuilder()
                    .WithDescription(buider.ToString())
                    .WithAuthor(User.Username + "#" + User.Discriminator, User.GetAvatarUrl() ?? User.GetDefaultAvatarUrl())
                    .WithThumbnailUrl(User.GetAvatarUrl() ?? User.GetDefaultAvatarUrl())
                    .Build();
                await ReplyAsync(embed: e);
                return;
            }
            if(id == default)
            {
                id = Context.User.Id;
            }
            var user = Context.Guild.GetUser(id);
            var roles = (user as SocketGuildUser).Roles
                .Where(x => x.Id != Context.Guild.EveryoneRole.Id && x.Color != Color.Default)
                .OrderByDescending(x => x.Position)
                .ThenByDescending(x => x.IsHoisted);

            var builder = new StringBuilder()
                .AppendLine("**\u276f User Information**")
                .AppendLine($"ID: {user.Id}")
                .AppendLine($"Profile: <@{user.Id}>")
                .AppendLine($"Roles: {string.Join(" ", roles.Select(x => x.Mention))}");
            var embed = new EmbedBuilder()
                .WithAuthor(await (user as SocketUser).GetFullUsername(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithDescription(builder.ToString());
            await ReplyAsync(embed: embed.Build());
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
