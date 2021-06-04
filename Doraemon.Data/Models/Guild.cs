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
        /// <summary>
        /// The ID of the guild.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The name of the guild.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Name { get; set; }
    }
}
