namespace Doraemon.Data.Models.Core
{
    public class ClaimMapCreationData
    {
        /// <summary>
        ///     See <see cref="ClaimMap.UserId"/>
        /// </summary>
        public ulong? UserId { get; set; }
        
        /// <summary>
        ///     See <see cref="ClaimMap.RoleId" />
        /// </summary>
        public ulong? RoleId { get; set; }

        /// <summary>
        ///     See <see cref="ClaimMap.Type" />
        /// </summary>
        public ClaimMapType Type { get; set; }

        internal ClaimMap ToEntity()
        {
            return new()
            {
                UserId = UserId,
                RoleId = RoleId,
                Type = Type
            };
        }
    }
}