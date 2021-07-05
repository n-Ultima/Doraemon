using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailTicketCreationData
    {
        /// <summary>
        /// See <see cref="ModmailTicket.Id"/>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// See <see cref="ModmailTicket.UserId"/>
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// See <see cref="ModmailTicket.DmChannelId"/>
        /// </summary>
        public ulong DmChannelId { get; set; }

        /// <summary>
        /// See <see cref="ModmailTicket.ModmailChannelId"/>
        /// </summary>
        public ulong ModmailChannelId { get; set; }

        internal ModmailTicket ToEntity()
            => new ModmailTicket()
            {
                Id = Id,
                UserId = UserId,
                DmChannelId = DmChannelId,
                ModmailChannelId = ModmailChannelId
            };
    }
}
