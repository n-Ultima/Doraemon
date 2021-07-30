using System.Linq;
using Disqord;
using Disqord.Gateway;

namespace Doraemon.Common.Extensions
{
    public static class GuildChannelExtensions
    {
        public static bool IsPublic(this CachedGuildChannel channel)
        {
            if (channel?.Client.GetGuild(channel.GuildId) is IGuild guild)
            {
                var currentUser = guild.GetMember(channel.GuildId);
                var permissions = currentUser.GetPermissions(channel);

                return permissions.Contains(Permission.ViewChannels);
            }

            return false;
        }
    }
}