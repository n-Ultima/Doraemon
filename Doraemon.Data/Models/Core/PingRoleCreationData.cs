using Disqord;

namespace Doraemon.Data.Models.Core
{
    public class PingRoleCreationData
    {
        /// <summary>
        ///     See <see cref="PingRole.Id" />
        /// </summary>
        public Snowflake Id { get; set; }

        /// <summary>
        ///     See <see cref="PingRole.Name" />
        /// </summary>
        public string Name { get; set; }

        internal PingRole ToEntity()
        {
            return new()
            {
                Id = Id,
                Name = Name
            };
        }
    }
}