using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Core
{
    public class GuildUser
    {
        public ulong Id { get; set; }

        public string Username { get; set; }

        public string Discriminator { get; set; }

        public bool IsModmailBlocked { get; set; }

    }
}
