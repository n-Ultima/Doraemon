using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Doraemon.Data.Models.Core
{
    public static class Interactions
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        /// <summary>
        /// Returns True if the first user can normally moderate the second user.
        /// </summary>
        /// <param name="Moderator"></param>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool CanModerate(this SocketUser Moderator, SocketGuildUser User)
        {
            return (Moderator as SocketGuildUser).Hierarchy > User.Hierarchy;
        }
        /// <summary>
        /// Returns if the user contains a role called "Moderator"
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsMod(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var ModeratorRole = guild.Roles.FirstOrDefault(x => x.Name == "Moderator");
            return (user as SocketGuildUser).Roles.Contains(ModeratorRole);
        }
        /// <summary>
        /// Returns if the user contains a role called "Administrator"
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsAdmin(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var AdminRole = guild.Roles.FirstOrDefault(x => x.Name == "Administratior");
            return (user as SocketGuildUser).Roles.Contains(AdminRole);
        }
        /// <summary>
        /// Returns if a user contains a role called "Staff"
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsStaff(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var StaffRole = guild.Roles.FirstOrDefault(x => x.Name == "Staff");
            return (user as SocketGuildUser).Roles.Contains(StaffRole);
        }
        /// <summary>
        /// Returns if a user can post discord invite links not present on the whitelist. Add whatever roles you feel are necessary to bypass the link filter.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool CanPostLinks(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var WK = guild.Roles.FirstOrDefault(x => x.Name == "Staff");
            var A = guild.Roles.FirstOrDefault(x => x.Name == "Associate");
            return (user as SocketGuildUser).Roles.Contains(WK)||(user as SocketGuildUser).Roles.Contains(A);
        }
    }
}
