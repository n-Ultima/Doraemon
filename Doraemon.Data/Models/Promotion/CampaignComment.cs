using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models.Promotion
{
    public class CampaignComment
    {
        /// <summary>
        /// The ID of the comment.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The ID of the campaign that the comment is being applied to.
        /// </summary>
        public string CampaignId { get; set; }
        /// <summary>
        /// The Content of the comment.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Content { get; set; }
        /// <summary>
        /// The user who wrote the comment.
        /// </summary>
        public ulong AuthorId { get; set; }
    }
}
