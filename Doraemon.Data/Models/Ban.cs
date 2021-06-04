using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Doraemon.Data.Models
{
    public class Ban
    {
        /// <summary>
        /// The guild that the user was banned in.
        /// </summary>
        public SocketGuild Guild;
        /// <summary>
        /// The user that was banned in.
        /// </summary>
        public SocketGuildUser User;
        /// <summary>
        /// The time when the temp ban should be revoked.
        /// </summary>
        public DateTime End;
    }
}
