using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class Tag
    {
        public string Id { get; set; }

        public ulong ownerId { get; set; }
        [Column(TypeName = "citext")]
        public string Name { get; set; }

        public string Response { get; set; }
    }
}
