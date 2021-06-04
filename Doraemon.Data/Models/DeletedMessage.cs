using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class DeletedMessage
    {
        /// <summary>
        /// The content of the deleted
        /// </summary>
        public string content;
        /// <summary>
        /// The user who sent the message originally.
        /// </summary>
        public ulong userid;
        /// <summary>
        /// The channel the message was sent in.
        /// </summary>
        public ulong channelid;
        /// <summary>
        /// The time that the message was sent.
        /// </summary>
        public DateTime time;
        /// <summary>
        /// The time that the message was deleted.
        /// </summary>
        public DateTime deleteTime;
    }
}
