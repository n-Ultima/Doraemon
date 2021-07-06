using Discord.WebSocket;
using Doraemon.Common;

namespace Doraemon.Data.Models.Core
{
    public static class Interactions
    {
        public static DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Returns True if the first user can normally moderate the second user.
        /// </summary>
        /// <param name="Moderator"></param>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool CanModerate(this SocketUser Moderator, SocketGuildUser User)
        {
            return (Moderator as SocketGuildUser).Hierarchy > User.Hierarchy;
        }

        /// <summary>
        ///     Returns if a user contains a role called "Staff"
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsStaff(this SocketUser user)
        {
            if (user is SocketGuildUser gUser)
                return gUser.GuildPermissions.ManageMessages;
            return false;
        }
    }
}