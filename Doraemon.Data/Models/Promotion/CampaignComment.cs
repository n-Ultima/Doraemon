using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Promotion
{
    public class CampaignComment
    {
        /// <summary>
        ///     The ID of the comment.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        ///     The ID of the campaign that the comment is being applied to.
        /// </summary>
        public string CampaignId { get; set; }

        /// <summary>
        ///     The Content of the comment.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Content { get; set; }

        /// <summary>
        ///     The user who wrote the comment.
        /// </summary>
        public Snowflake AuthorId { get; set; }
    }
    public class CampaignCommentConfigurator : IEntityTypeConfiguration<CampaignComment>
    {
        public void Configure(EntityTypeBuilder<CampaignComment> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.AuthorId)
                .HasConversion<ulong>();
        }
    }
}