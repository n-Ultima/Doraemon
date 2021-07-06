using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models.Core
{
    public class Guild
    {
        /// <summary>
        ///     The ID of the guild.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The name of the guild.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Name { get; set; }
    }
}