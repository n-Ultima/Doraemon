using Disqord;

namespace Doraemon.Data.Models.Core
{
    public class GuildUserCreationData
    {
        /// <summary>
        ///     See <see cref="GuildUser.Id" />.
        /// </summary>
        public Snowflake Id { get; set; }

        /// <summary>
        ///     See <see cref="GuildUser.Username" />.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     See <see cref="GuildUser.Discriminator" />.
        /// </summary>
        public string Discriminator { get; set; }

        internal GuildUser ToEntity()
        {
            return new()
            {
                Id = Id,
                Username = Username,
                Discriminator = Discriminator,
            };
        }
    }
}