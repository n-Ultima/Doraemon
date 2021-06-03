using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class Campaign
    {
        public ulong userId { get; set; }
        public ulong initiatorId { get; set; }
        public string Id { get; set; }
        public string ReasonForCampaign { get; set; }
    }
}
