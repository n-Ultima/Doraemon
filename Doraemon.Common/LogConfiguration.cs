namespace Doraemon.Common
{
    public class LogConfiguration
    {
        /// <summary>
        /// The channel ID that moderation-actions will be logged to.
        /// </summary>
        public ulong ModLogChannelId { get; set; }

        /// <summary>
        /// The channel ID that user-joins will be logged to.
        /// </summary>
        public ulong UserJoinedLogChannelId { get; set; }

        /// <summary>
        /// The channel ID that message updates will be logged to.
        /// </summary>
        public ulong MessageLogChannelId { get; set; }

        /// <summary>
        /// The channel ID that promotion logs will be logged to.
        /// </summary>
        public ulong PromotionLogChannelId { get; set; }
    }
}
