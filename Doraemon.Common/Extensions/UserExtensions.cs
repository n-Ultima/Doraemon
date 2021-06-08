using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Doraemon.Common.Extensions
{
    public static class UserExtensions
    {
        public static async Task<string> GetFullUsername(this SocketUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }
        public static async Task<string> GetFullUsername(this RestUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }
    }
}
