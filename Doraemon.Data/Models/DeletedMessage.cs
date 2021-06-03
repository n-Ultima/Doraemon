using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class DeletedMessage
    {
        public string content;
        public ulong userid;
        public ulong channelid;
        public DateTime time;
        public DateTime deleteTime;
    }
}
