using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
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
        public string campaignId { get; set; }
        /// <summary>
        /// The content of the comment.
        /// </summary>
        [Column(TypeName = "citext")]
        public string content { get; set; }
        /// <summary>
        /// The user who wrote the comment.
        /// </summary>
        public ulong authorId { get; set; }
    }
}
