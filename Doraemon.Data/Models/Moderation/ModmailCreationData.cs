using Disqord;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailTicketCreationData
    {
        /// <summary>
        ///     See <see cref="ModmailTicket.Id" />
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     See <see cref="ModmailTicket.UserId" />
        /// </summary>
        public Snowflake UserId { get; set; }

        /// <summary>
        ///     See <see cref="ModmailTicket.DmChannelId" />
        /// </summary>
        public Snowflake DmChannelId { get; set; }

        /// <summary>
        ///     See <see cref="ModmailTicket.ModmailChannelId" />
        /// </summary>
        public Snowflake ModmailChannelId { get; set; }

        internal ModmailTicket ToEntity()
        {
            return new()
            {
                Id = Id,
                UserId = UserId,
                DmChannelId = DmChannelId,
                ModmailChannelId = ModmailChannelId,
            };
        }
    }
}