using Disqord;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailMessageCreationData
    {
        public string TicketId { get; set; }
        
        public Snowflake AuthorId { get; set; }
        
        public Snowflake MessageId { get; set; }
        public string Content { get; set; }

        internal ModmailMessage ToEntity()
            => new ModmailMessage()
            {
                TicketId = TicketId,
                AuthorId = AuthorId,
                Content = Content,
                MessageId = MessageId
            };
    }
}