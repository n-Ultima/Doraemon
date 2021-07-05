using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Core
{
    public class PingRoleCreationData
    {
        /// <summary>
        /// See <see cref="PingRole.Id"/>
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// See <see cref="PingRole.Name"/>
        /// </summary>
        public string Name { get; set; }

        internal PingRole ToEntity()
            => new PingRole()
            {
                Id = Id,
                Name = Name
            };
    }
}
