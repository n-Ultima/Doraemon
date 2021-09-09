using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailMessage
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        public Snowflake MessageId { get; set; }
        
        public string TicketId { get; set; }
        
        public Snowflake AuthorId { get; set; }
        
        public string Content { get; set; }
    }
    
    public class ModmailMessageConfigurator : IEntityTypeConfiguration<ModmailMessage>
    {
        public void Configure(EntityTypeBuilder<ModmailMessage> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.AuthorId)
                .HasConversion<ulong>();
        }
    }
}