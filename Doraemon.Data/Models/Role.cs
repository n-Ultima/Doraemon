using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class Role
    {
        // RoleID
        public ulong Id { get; set; }
        // Name of the role.
        [Column(TypeName = "citext")]
        public string Name { get; set; }
        // Description to go along each role.
        public string Description { get; set; }

        public string T2 { get; set; }
    }
}
