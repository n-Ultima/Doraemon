using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Promotion
{
    public class Campaign
    {
        /// <summary>
        ///     The user who is being nominated.
        /// </summary>
        public Snowflake UserId { get; set; }

        /// <summary>
        ///     The user who initiated the ID.
        /// </summary>
        public Snowflake InitiatorId { get; set; }

        /// <summary>
        ///     The ID of the campaign.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The reason for the campaign being initiated.
        /// </summary>
        public string ReasonForCampaign { get; set; }
    }
    public class CampaignConfigurator : IEntityTypeConfiguration<Campaign>
    {
        public void Configure(EntityTypeBuilder<Campaign> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.UserId)
                .HasConversion<ulong>();
            entityTypeBuilder
                .Property(x => x.InitiatorId)
                .HasConversion<ulong>();
        }
    }
}