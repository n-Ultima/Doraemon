using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class Infraction
    {
        /// <summary>
        /// The moderator that issued the infraction.
        /// </summary>
        public ulong ModeratorId { get; set; }
        /// <summary>
        /// The user that is being issued the infraction.
        /// </summary>
        public ulong SubjectId { get; set; }
        /// <summary>
        /// The ID of the infraction.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The reason for the infraction being given.
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// The type of infraction.
        /// </summary>
        public string Type { get; set; }
    }
}
