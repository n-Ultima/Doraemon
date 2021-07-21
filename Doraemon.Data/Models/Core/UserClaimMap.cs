using System.ComponentModel.DataAnnotations.Schema;

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
        public ulong UserId { get; set; }

        /// <summary>
        ///     The type of claim actually being implemented into the user.
        /// </summary>
        public ClaimMapType Type { get; set; }
    }
}