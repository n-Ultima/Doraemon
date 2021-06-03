using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class Guild
    {
        public string Id { get; set; }
        [Column(TypeName = "citext")]

        public string Name { get; set; }
    }
}
