using Discord.Rest;
using Discord.WebSocket;

namespace Doraemon.Common.Extensions
{
    public static class UserExtensions
    {
        public static string GetFullUsername(this SocketUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }

        public static string GetFullUsername(this RestUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }

        public static string GetDefiniteAvatarUrl(this SocketUser user)
        {
            return user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        }

        public static string GetDefiniteAvatarUrl(this RestUser user)
        {
            return user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        }
    }
}