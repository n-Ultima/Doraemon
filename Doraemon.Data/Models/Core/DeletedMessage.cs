using System;

namespace Doraemon.Data.Models.Core
{
    public class DeletedMessage
    {
        /// <summary>
        ///     The channel the message was sent in.
        /// </summary>
        public ulong ChannelId;

        /// <summary>
        ///     The Content of the deleted
        /// </summary>
        public string Content;

        /// <summary>
        ///     The Time that the message was deleted.
        /// </summary>
        public DateTime DeleteTime;

        /// <summary>
        ///     The Time that the message was sent.
        /// </summary>
        public DateTime Time;

        /// <summary>
        ///     The user who sent the message originally.
        /// </summary>
        public ulong UserId;
    }
}