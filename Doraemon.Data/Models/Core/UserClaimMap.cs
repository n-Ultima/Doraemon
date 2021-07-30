using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models.Core
{
    public class UserClaimMap
    {
        /// <summary>
        ///     The ID of the entry to the database.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        ///     The UserID receiving the claim.
        /// </summary>
        public Snowflake UserId { get; set; }

        /// <summary>
        ///     The type of claim actually being implemented into the user.
        /// </summary>
        public ClaimMapType Type { get; set; }
    }
    public class UserConfigurator : IEntityTypeConfiguration<UserClaimMap>
    {
        public void Configure(EntityTypeBuilder<UserClaimMap> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.UserId)
                .HasConversion<ulong>();
        }
    }
}