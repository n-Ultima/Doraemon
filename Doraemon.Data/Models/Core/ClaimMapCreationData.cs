namespace Doraemon.Data.Models.Core
{
    public class ClaimMapCreationData
    {
        /// <summary>
        ///     See <see cref="ClaimMap.RoleId" />
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        ///     See <see cref="ClaimMap.Type" />
        /// </summary>
        public ClaimMapType Type { get; set; }

        internal ClaimMap ToEntity()
        {
            return new()
            {
                RoleId = RoleId,
                Type = Type
            };
        }
    }
}