using System;

namespace Doraemon.Data.Models.Core
{
    public class DeletedMessage
    {
        /// <summary>
        ///     The channel the message was sent in.
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        ///     The Content of the deleted message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     The Time that the message was deleted.
        /// </summary>
        public DateTime DeleteTime { get; set; }

        /// <summary>
        ///     The Time that the message was sent.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        ///     The user who sent the message originally.
        /// </summary>
        public ulong UserId { get; set; }
    }
}