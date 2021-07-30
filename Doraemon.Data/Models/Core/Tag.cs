using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doraemon.Data.Models
{
    public class Tag
    {
        /// <summary>
        ///     The ID of the tag.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The user who owns the tag.
        /// </summary>
        public Snowflake OwnerId { get; set; }

        /// <summary>
        ///     The name of the tag.
        /// </summary>

        [Column(TypeName = "citext")]
        public string Name { get; set; }

        /// <summary>
        ///     The response that the tag will hold.
        /// </summary>
        public string Response { get; set; }
    }
    public class TagConfigurator : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> entityTypeBuilder)
        {
            entityTypeBuilder
                .Property(x => x.OwnerId)
                .HasConversion<ulong>();
        }
    }
}