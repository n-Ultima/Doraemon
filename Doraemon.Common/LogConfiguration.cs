namespace Doraemon.Common
{
    public class LogConfiguration
    {
        /// <summary>
        ///     The channel ID that moderation-actions will be logged to.
        /// </summary>
        public ulong ModLogChannelId { get; set; }

        /// <summary>
        ///     The channel ID that user-joins will be logged to.
        /// </summary>
        public ulong UserJoinedLogChannelId { get; set; }

        /// <summary>
        ///     The channel ID that message updates will be logged to.
        /// </summary>
        public ulong MessageLogChannelId { get; set; }

        /// <summary>
        ///     The channel ID that promotion logs will be logged to.
        /// </summary>
        public ulong PromotionLogChannelId { get; set; }

        /// <summary>
        ///     Sets if logs should be used in embed or text form. Only "EMBED" and "TEXT" are valid options.
        /// </summary>
        public string EmbedOrText { get; set; }
        /// <summary>
        ///     The channel ID where everything else that needs logged will be sent to.
        /// </summary>
        public ulong MiscellaneousLogChannelId { get; set; }
    }
}