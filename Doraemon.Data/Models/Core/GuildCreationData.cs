using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Core
{
    public class GuildCreationData
    {
        /// <summary>
        /// See <see cref="Guild.Id"/>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// See <see cref="Guild.Name"/>
        /// </summary>
        public string Name { get; set; }

        internal Guild ToEntity()
            => new Guild()
            {
                Id = Id,
                Name = Name
            };
    }
}
