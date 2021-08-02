//using System.Linq;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;
//using Discord.WebSocket;
//using Doraemon.Common.Extensions;
//using Doraemon.Data.TypeReaders;
//using Humanizer;
//
//namespace Doraemon.Modules
//{
//    public class UserInfo : ModuleBase<SocketCommandContext>
//    {
//        private readonly DiscordSocketClient _client;
//
//        public UserInfo(DiscordSocketClient client)
//        {
//            _client = client;
//        }
//
//        [Command("info")]
//        [Summary("Displays info about the user, or the author if none is provided.")]
//        public async Task DisplayUserInfoAsync(
//            [Summary("The user to query for information.")]
//                SocketGuildUser user = null)
//        {
//            if (user == null) user = Context.User as SocketGuildUser;
//            var hierarchy = user.Hierarchy;
//            var roles = user.Roles
//                .Where(x => x.Id != Context.Guild.EveryoneRole.Id && x.Color != Color.Default)
//                .OrderByDescending(x => x.Position)
//                .ThenByDescending(x => x.IsHoisted)
//                .Select(x => x.Mention);
//            var embed = new EmbedBuilder()
//                .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
//                .AddField("Creation", user.CreatedAt.ToString("d"), true)
//                .AddField("Joined Server", user.JoinedAt.Value.ToString("f"), true)
//                .AddField("Username", user.Username, true)
//                .AddField("Discriminator", user.Discriminator, true)
//                .AddField("ID", user.Id, true)
//                .AddField("Hierarchy", hierarchy == int.MaxValue
//                    ? "Guild Owner"
//                    : hierarchy.ToString(), true)
//                .AddField("Roles", roles.Humanize())
//                .WithColor(Color.DarkBlue)
//                .Build();
//            await ReplyAsync(embed: embed);
//        }
//
//        [Command("info")]
//        public async Task DisplayUserInfoAsync(ulong userId)
//        {
//            var user = await _client.Rest.GetUserAsync(userId);
//            var gUser = Context.Guild.GetUser(userId);
//            if (gUser != null)
//            {
//                var hierarchy = gUser.Hierarchy;
//                var roles = gUser.Roles
//                    .Where(x => x.Id != Context.Guild.EveryoneRole.Id && x.Color != Color.Default)
//                    .OrderByDescending(x => x.Position)
//                    .ThenByDescending(x => x.IsHoisted)
//                    .Select(x => x.Mention);
//                var embed = new EmbedBuilder()
//                    .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
//                    .AddField("Creation", user.CreatedAt.ToString("d"), true)
//                    .AddField("Joined Server", gUser.JoinedAt.Value.ToString("f"), true)
//                    .AddField("Username", user.Username, true)
//                    .AddField("Discriminator", user.Discriminator, true)
//                    .AddField("ID", user.Id, true)
//                    .AddField("Hierarchy", hierarchy == int.MaxValue
//                        ? "Guild Owner"
//                        : hierarchy.ToString(), true)
//                    .AddField("Roles", roles.Humanize())
//                    .WithColor(Color.DarkBlue)
//                    .Build();
//                await ReplyAsync(embed: embed);
//            }
//            else
//            {
//                var embed = new EmbedBuilder()
//                    .WithAuthor(user.GetFullUsername(), user.GetDefiniteAvatarUrl())
//                    .AddField("Creation", user.CreatedAt.ToString("d"), true)
//                    .AddField("Username", user.Username, true)
//                    .AddField("Discriminator", user.Discriminator, true)
//                    .AddField("ID", user.Id, true)
//                    .WithColor(Color.DarkBlue)
//                    .Build();
//                await ReplyAsync(embed: embed);
//            }
//        }
//        [Command("avatar")]
//        [Summary("Gets a user's avatar.")]
//        public async Task GetAvatarAsync(
//            [Summary("The user whose avatar to be displayed.")]
//                SocketGuildUser user)
//        {
//            var avatar = user.GetAvatarUrl(size: 2048) ?? user.GetDefaultAvatarUrl();
//            var embed = new EmbedBuilder()
//                .WithImageUrl(avatar)
//                .WithTitle($"Avatar of {user.GetFullUsername()}")
//                .Build();
//            await ReplyAsync(embed: embed);
//        }
//
//        [Command("avatar")]
//        [Priority(10)]
//        [Summary("Gets a user's avatar.")]
//        public async Task GetAvatarAsync(
//            [Summary("The ID of the user whose avatar to display.")]
//                ulong userId)
//        {
//            var user = await _client.Rest.GetUserAsync(userId);
//            var avatar = user.GetAvatarUrl(ImageFormat.Auto, 2048) ?? user.GetDefaultAvatarUrl();
//            var e = new EmbedBuilder()
//                .WithImageUrl(avatar)
//                .WithTitle($"Avatar of {user.GetFullUsername()}")
//                .Build();
//            await ReplyAsync(embed: e);
//        }
//    }
//}