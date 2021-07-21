namespace Doraemon.Data.Models.Core
{
    public class UserClaimMapCreationData
    {
        
        /// <summary>
        ///     See <see cref="UserClaimMap.UserId"/>
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        ///     See <see cref="UserClaimMap.Type"/>
        /// </summary>
        public ClaimMapType Type { get; set; }

        internal UserClaimMap ToEntity()
            => new UserClaimMap()
            {
                UserId = UserId,
                Type = Type
            };
    }
}