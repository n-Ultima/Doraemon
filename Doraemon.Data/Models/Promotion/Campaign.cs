namespace Doraemon.Data.Models.Promotion
{
    public class Campaign
    {
        /// <summary>
        ///     The user who is being nominated.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        ///     The user who initiated the ID.
        /// </summary>
        public ulong InitiatorId { get; set; }

        /// <summary>
        ///     The ID of the campaign.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The reason for the campaign being initiated.
        /// </summary>
        public string ReasonForCampaign { get; set; }
    }
}