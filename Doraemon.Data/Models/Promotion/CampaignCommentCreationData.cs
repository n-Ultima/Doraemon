namespace Doraemon.Data.Models.Promotion
{
    public class CampaignCommentCreationData
    {
        /// <summary>
        ///     See <see cref="CampaignComment.Id" />.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     See <see cref="CampaignComment.CampaignId" />.
        /// </summary>
        public string CampaignId { get; set; }

        /// <summary>
        ///     See <see cref="CampaignComment.Content" />.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     See <see cref="CampaignComment.AuthorId" />.
        /// </summary>
        public ulong AuthorId { get; set; }

        internal CampaignComment ToEntity()
        {
            return new()
            {
                Id = Id,
                CampaignId = CampaignId,
                Content = Content,
                AuthorId = AuthorId
            };
        }
    }
}