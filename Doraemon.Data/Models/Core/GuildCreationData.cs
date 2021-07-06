namespace Doraemon.Data.Models.Core
{
    public class GuildCreationData
    {
        /// <summary>
        ///     See <see cref="Guild.Id" />
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     See <see cref="Guild.Name" />
        /// </summary>
        public string Name { get; set; }

        internal Guild ToEntity()
        {
            return new()
            {
                Id = Id,
                Name = Name
            };
        }
    }
}