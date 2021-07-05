using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class PingRole
    {
        /// <summary>
        /// The ID of the role.
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// The name of the role.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Name { get; set; }
    }
}
