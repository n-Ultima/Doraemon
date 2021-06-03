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
        // The guild the user was muted in
        public SocketGuild Guild;
        // The user muted
        public SocketGuildUser User;
        // The muted role
        public IRole Role;
        // When the user should be unmuted.
        public DateTime End;
    }
}

