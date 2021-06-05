using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace Doraemon.Data.Models
{
    public class Mute
    {
        /// <summary>
        /// The guild the user was muted in.
        /// </summary>
        public SocketGuild Guild;
        /// <summary>
        /// The user that will be muted.
        /// </summary>
        public SocketGuildUser User;
        /// <summary>
        /// The muted role.
        /// </summary>
        public IRole Role;
        /// <summary>
        /// The Time that the mute should end.
        /// </summary>
        public DateTime End;
    }
}

