using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class ModmailTicket
    {
        /// <summary>
        /// I only declare the ID here to satisfy Ef core.
        /// </summary>
        public string Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ModmailChannel { get; set; }
        public ulong DmChannel { get; set; }
    }
}
