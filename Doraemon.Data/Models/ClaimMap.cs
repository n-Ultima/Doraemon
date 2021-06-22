using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class ClaimMap
    {
        /// <summary>
        /// The ID of the entry to the database.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// The RoleID receiving the claim.
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        /// The type of claim actually being implemented into the role.
        /// </summary>
        public ClaimMapType Type { get; set; }
    }
}
