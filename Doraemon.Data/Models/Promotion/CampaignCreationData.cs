namespace Doraemon.Data.Models.Promotion
{
    public class CampaignCreationData
    {
        /// <summary>
        ///     See <see cref="Campaign.UserId" />
        /// </summary>

        public ulong UserId { get; set; }

        /// <summary>
        ///     See <see cref="Campaign.InitiatorId" />
        /// </summary>
        public ulong InitiatorId { get; set; }

        /// <summary>
        ///     See <see cref="Campaign.Id" />
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     See <see cref="Campaign.ReasonForCampaign" />
        /// </summary>
        public string ReasonForCampaign { get; set; }

        internal Campaign ToEntity()
        {
            return new()
            {
                Id = Id,
                UserId = UserId,
                InitiatorId = InitiatorId,
                ReasonForCampaign = ReasonForCampaign
            };
        }
    }
}