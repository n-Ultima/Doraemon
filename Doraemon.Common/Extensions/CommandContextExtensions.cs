using System;
using System.Linq;
using System.Threading.Tasks;

using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;

namespace Doraemon.Common.Extensions
{
    public static class CommandContextExtension
    {
        public static LocalEmoji Success = new("✅");
        private static readonly LocalEmoji _xEmoji = new("❌");
        private const int _confirmationTimeoutSeconds = 10;

        public static async Task AddConfirmationAsync(this IUserMessage message, CachedGuildChannel channel)
        {
            if (channel != null)
            {
                var guild = channel.Client.GetGuild(channel.GuildId);
                var currentUser = guild.GetMember(guild.Client.CurrentUser.Id);
                var permissions = currentUser.GetPermissions(channel);
                if (!permissions.AddReactions)
                {
                    await (channel as ITextChannel).SendMessageAsync(new LocalMessage()
                        .WithContent($"I was unable to add the ✅ to your message due to the `Add Reactions` not being allowed to me."));
                    return;
                }
            }

            await message.AddReactionAsync(Success);
        }
        public static bool IsPublic(this IGuildChannel channel)
        {
            var guild = (channel.Client as DiscordBotBase).GetGuild(channel.GuildId);
            var everyoneRole = guild.Roles.FirstOrDefault(x => x.Value.Id == guild.Id).Value;
            var permissions = channel.Overwrites.FirstOrDefault(x => x.TargetType == OverwriteTargetType.Role && x.TargetId == everyoneRole.Id);
            if (permissions == null)
            {
                return true; // if it isn't set, return false
            }
            else
            {
                if (permissions.Permissions.Allowed.ViewChannels)
                {
                    return true;
                }

                if (!permissions.Permissions.Allowed.ViewChannels && !permissions.Permissions.Denied.ViewChannels)
                {
                    return true;
                }
                if (permissions.Permissions.Allowed.ViewChannels && permissions.Permissions.Denied.ViewChannels)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}