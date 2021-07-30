using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models
{
    public class PingRole
    {
        /// <summary>
        ///     The ID of the role.
        /// </summary>
        public Snowflake Id { get; set; }

        /// <summary>
        ///     The name of the role.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Name { get; set; }
    }

    public class PingRoleConfigurator : IEntityTypeConfiguration<PingRole>
    {
        public void Configure(EntityTypeBuilder<PingRole> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.Id)
                .HasConversion<ulong>();
        }
    }
}