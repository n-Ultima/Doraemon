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
        // Make sure the user using the command isn't trying to moderate someone higher. Like a mod banning an admin.
        public static bool CanModerate(this SocketUser Moderator, SocketGuildUser User)
        {
            return (Moderator as SocketGuildUser).Hierarchy > User.Hierarchy;
        }
        // Check if a user is a mod.
        public static bool IsMod(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var ModeratorRole = guild.GetRole(811781666099167332);
            return (user as SocketGuildUser).Roles.Contains(ModeratorRole);
        }
        // Check if a user is an admin
        public static bool IsAdmin(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var AdminRole = guild.Roles.FirstOrDefault(x => x.Name == "Administratior");
            return (user as SocketGuildUser).Roles.Contains(AdminRole);
        }
        // Check if a user is staff in general.
        public static bool IsStaff(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var StaffRole = guild.Roles.FirstOrDefault(x => x.Name == "Staff");
            return (user as SocketGuildUser).Roles.Contains(StaffRole);
        }
        public static bool CanPostLinks(this SocketUser user)
        {
            var guild = CommandHandler._client.GetGuild(DoraemonConfig.MainGuildId);
            var WK = guild.GetRole(815746947994353695);
            return (user as SocketGuildUser).Roles.Contains(WK);
        }
    }
}
