using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailMessage
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        public string TicketId { get; set; }
        
        public ulong AuthorId { get; set; }
        
        public string Content { get; set; }
    }
}