namespace Doraemon.Data.Models.Core
{
    public class RoleClaimMapCreationData
    {
        /// <summary>
        ///     See <see cref="RoleClaimMap.RoleId" />
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        ///     See <see cref="RoleClaimMap.Type" />
        /// </summary>
        public ClaimMapType Type { get; set; }

        internal RoleClaimMap ToEntity()
        {
            return new()
            {
                RoleId = RoleId,
                Type = Type
            };
        }
    }
}