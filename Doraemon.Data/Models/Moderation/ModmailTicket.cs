using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailTicket
    {
        /// <summary>
        /// I only declare the ID here to satisfy Ef core.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The user's ID who contacted Modmail
        /// </summary>
        public ulong UserId { get; set; }
        /// <summary>
        /// The channel inside of the guild that Staff will use to respond to the ticket.
        /// </summary>
        public ulong ModmailChannelId { get; set; }
        /// <summary>
        /// The corresponding DM channel.
        /// </summary>
        public ulong DmChannelId { get; set; }
    }
}
