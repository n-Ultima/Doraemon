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
        // The guild the user was banned from
        public SocketGuild Guild;
        // The user who was banned
        public SocketGuildUser User;
        // When the ban should be revoked
        public DateTime End;
    }
}
