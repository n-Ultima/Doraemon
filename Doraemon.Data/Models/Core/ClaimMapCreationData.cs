using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Core
{
    public class ClaimMapCreationData
    {
        /// <summary>
        /// See <see cref="ClaimMap.RoleId"/>
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        /// See <see cref="ClaimMap.Type"/>
        /// </summary>
        public ClaimMapType Type { get; set; }

        internal ClaimMap ToEntity()
            => new ClaimMap()
            {
                RoleId = RoleId,
                Type = Type
            };
    }
}
