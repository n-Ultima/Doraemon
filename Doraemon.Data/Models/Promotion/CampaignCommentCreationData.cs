using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Promotion
{
    public class CampaignCommentCreationData
    {
        /// <summary>
        /// See <see cref="CampaignComment.Id"/>.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// See <see cref="CampaignComment.CampaignId"/>.
        /// </summary>
        public string CampaignId { get; set; }
        /// <summary>
        /// See <see cref="CampaignComment.Content"/>.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// See <see cref="CampaignComment.AuthorId"/>.
        /// </summary>
        public ulong AuthorId { get; set; }

        internal CampaignComment ToEntity()
            => new CampaignComment()
            {
                Id = Id,
                CampaignId = CampaignId,
                Content = Content,
                AuthorId = AuthorId
            };
    }
}
