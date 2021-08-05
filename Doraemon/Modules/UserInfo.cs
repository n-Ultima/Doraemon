using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Humanizer;
using Qmmands;

namespace Doraemon.Modules
{
    public class UserInfo : DoraemonGuildModuleBase
    {
        [Command("info")]
        [Description("Displays info about the user, or the author if none is provided.")]
        public DiscordCommandResult DisplayUserInfo(
            [Description("The user to query for information.")]
            IMember user)
        {
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
                .WithColor(DColor.DarkBlue);
            if (roles.Any())
            {
                embed.AddField("Roles", roles.Humanize(), true);
            }
            else
            {
                embed.AddField("Roles", "No roles.");
            }

            return Response(embed);
        }

        [Command("info")]
        [Description("Displays info about the executing user.")]
        public DiscordCommandResult DisplayUserInfo()
        {
            var user = Context.Author;
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
                .WithColor(DColor.DarkBlue);
            if (roles.Any())
            {
                embed.AddField("Roles", roles.Humanize(), true);
            }
            else
            {
                embed.AddField("Roles", "No roles.");
            }

            return Response(embed);
        }

        [Command("info")]
        [Description("Displays info about the executing user.")]
        public async Task<DiscordCommandResult> DisplayUserInfoAsync(
            [Description("The user ID to get information about.")]
            Snowflake userId)
        {
            var user = await Bot.FetchUserAsync(userId);
            if (Context.Guild.GetMember(user.Id) != null)
            {
                await DisplayUserInfo(Context.Guild.GetMember(user.Id));
                return null;
            }

            var embed = new LocalEmbed()
                .WithAuthor(user)
                .AddField("Creation", user.CreatedAt().ToString("d"), true)
                .AddField("Username", user.Name, true)
                .AddField("Discriminator", user.Discriminator, true)
                .AddField("ID", user.Id, true)
                .WithColor(DColor.DarkBlue);
            return Response(embed);
        }

        [Command("avatar")]
        [Description("Gets a user's avatar.")]
        public DiscordCommandResult GetAvatar(
            [Description("The user whose avatar to be displayed.")]
            IMember user)
        {
            var avatar = user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048) ?? user.GetDefaultAvatarUrl();
            var embed = new LocalEmbed()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.Tag}");
            return Response(embed);
        }

        [Command("avatar")]
        [Priority(10)]
        [Description("Gets a user's avatar.")]
        public async Task<DiscordCommandResult> GetAvatarAsync(
            [Description("The ID of the user whose avatar to display.")]
            Snowflake userId)
        {
            var user = await Bot.FetchUserAsync(userId);
            if (Context.Guild.GetMember(user.Id) != null)
            {
                var member = (Context.Guild.GetMember(user.Id) as IMember);
                await GetAvatar(member);
                return null;
            }

            var avatar = user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048) ?? user.GetDefaultAvatarUrl();
            var e = new LocalEmbed()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.Tag}");
            return Response(e);
        }

        [Command("avatar")]
        [Description("Gets the avatar of the command executor.")]
        public DiscordCommandResult GetAvatar()
        {
            var user = Context.Author;
            var avatar = user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048) ?? user.GetDefaultAvatarUrl();
            var e = new LocalEmbed()
                .WithImageUrl(avatar)
                .WithTitle($"Avatar of {user.Tag}");
            return Response(e);
        }
    }
}