using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Data.TypeReaders;
using Humanizer;
using Qmmands;

namespace Doraemon.Modules
{
    public class UserInfo : DiscordGuildModuleBase
    {
        [Command("info")]
        [Description("Displays info about the user, or the author if none is provided.")]
        public async Task DisplayUserInfoAsync(
            [Description("The user to query for information.")]
                IMember user = null)
        {
            if (user == null) user = Context.Author;
            var hierarchy = user.GetHierarchy();
            var roles = user.GetRoles()
                .Select(x => x.Value)
                .Where(x => x.Id != Context.Guild.Id)
                .OrderByDescending(x => x.Position)
                .ThenByDescending(x => x.IsHoisted)
                .Select(x => x.Mention);

            var embed = new LocalEmbed()
                .WithAuthor(user)
                .AddField("Creation", user.CreatedAt().ToString("d"), true)
                .AddField("Joined Server", user.JoinedAt.Value.ToString("f"), true)
                .AddField("Username", user.Name, true)
                .AddField("Discriminator", user.Discriminator, true)
                .AddField("ID", user.Id, true)
                .AddField("Hierarchy", hierarchy == int.MaxValue
                    ? "Guild Owner"
                    : hierarchy.ToString(), true)
                .WithColor(Color.DarkBlue);
            if (roles.Any())
            {
                embed.AddField("Roles", roles.Humanize(), true);
            }
            else
            {
                embed.AddField("Roles", "No roles.");
            }
            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
        }
        [Command("avatar")]
        [Description("Gets a user's avatar.")]
        public async Task GetAvatarAsync(
            [Description("The user whose avatar to be displayed.")]
                IMember user)
        {
            var avatar = user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048) ?? user.GetDefaultAvatarUrl();
            var embed = new LocalEmbed()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.Tag}");
            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
        }

        [Command("avatar")]
        [Priority(10)]
        [Description("Gets a user's avatar.")]
        public async Task GetAvatarAsync(
            [Description("The ID of the user whose avatar to display.")]
                Snowflake userId)
        {
            var user = await Bot.FetchUserAsync(userId);
            var avatar = user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048) ?? user.GetDefaultAvatarUrl();
            var e = new LocalEmbed()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.Tag}");
            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(e));
        }
    }
}