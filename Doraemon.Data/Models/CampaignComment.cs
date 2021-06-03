using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models
{
    public class CampaignComment
    {
        public string Id { get; set; }
        public string campaignId { get; set; }
        [Column(TypeName = "citext")]
        public string content { get; set; }
        public ulong authorId { get; set; }
    }
}
