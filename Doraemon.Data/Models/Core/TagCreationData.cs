using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models.Core
{
    public class TagCreationData
    {
        /// <summary>
        /// See <see cref="Tag.Id"/>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// See <see cref="Tag.OwnerId"/>
        /// </summary>
        public ulong OwnerId { get; set; }
        /// <summary>
        /// See <see cref="Tag.Name"/>
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// See <see cref="Tag.Response"/>
        /// </summary>
        public string Response { get; set; }

        internal Tag ToEntity()
            => new Tag()
            {
                Id = Id,
                OwnerId = OwnerId,
                Name = Name,
                Response = Response
            };
    }
}
