using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Core
{
    public class RoleClaimMap
    {
        /// <summary>
        ///     The ID of the entry to the database.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        ///     The RoleID receiving the claim.
        /// </summary>
        public Snowflake RoleId { get; set; }

        /// <summary>
        ///     The type of claim actually being implemented into the role.
        /// </summary>
        public ClaimMapType Type { get; set; }
    }
    public class RoleClaimMapConfigurator : IEntityTypeConfiguration<RoleClaimMap>
    {
        public void Configure(EntityTypeBuilder<RoleClaimMap> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.Id)
                .HasConversion<ulong>();
        }
    }
}