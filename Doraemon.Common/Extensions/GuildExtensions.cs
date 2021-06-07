using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Common.Extensions
{
    public  static class GuildChannelExtensions
    {
        public static bool IsPublic(this IGuildChannel channel)
        {
            if (channel?.Guild is IGuild guild)
            {
                var permissions = channel.GetPermissionOverwrite(guild.EveryoneRole);

                return !permissions.HasValue || permissions.Value.ViewChannel != PermValue.Deny;
            }

            return false;
        }
    }
}
