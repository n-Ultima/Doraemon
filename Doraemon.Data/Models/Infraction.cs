using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class Infraction
    {
        // The person issuing the warn
        public ulong moderatorId { get; set; }
        // The person receiving the warn
        public ulong subjectId { get; set; }
        // The caseId
        public string Id { get; set; }
        // The reason for the infraction
        public string Reason { get; set; }
        // The type of infraction
        public string Type { get; set; }
    }
}
