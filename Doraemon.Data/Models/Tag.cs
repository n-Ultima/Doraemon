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
        /// <summary>
        /// The ID of the tag.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The user who owns the tag.
        /// </summary>
        public ulong OwnerId { get; set; }
        /// <summary>
        /// The name of the tag.
        /// </summary>
        
        [Column(TypeName = "citext")]
        public string Name { get; set; }
        /// <summary>
        /// The response that the tag will hold.
        /// </summary>
        public string Response { get; set; }
    }
}
