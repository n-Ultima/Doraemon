using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Models
{
    public class GuildRole
    {
        /// <summary>
        /// The ID of the role itself.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// The list of claims that the role has.
        /// </summary>
        
        public string Name { get; set; }

        public int Position { get; set; }
    }

    public enum ClaimMapType 
    {
        /// <summary>
        /// Allows the role bearer to read infraction history.
        /// </summary>
        InfractionView,
        /// <summary>
        /// Allows the role bearer to create infractions.
        /// </summary>
        InfractionCreate,
        /// <summary>
        /// Allows the role bearer to delete infractions.
        /// </summary>
        InfractionDelete,
        /// <summary>
        /// Allows the role bearer to update infractions.
        /// </summary>
        InfractionUpdate,
        /// <summary>
        /// Allows the role bearer to read on-going promotions.
        /// </summary>
        PromotionRead,
        /// <summary>
        /// Allows the role bearer to start campaigns.
        /// </summary>
        PromotionStart,
        /// <summary>
        /// Allows the role bearer to accept or deny campaigns.
        /// </summary>
        PromotionManage,
        /// <summary>
        /// Allows the role bearer to comment on an ongoing campaign.
        /// </summary>
        PromotionComment,
        /// <summary>
        /// Allows the role bearer to create, edit, and delete tags that they own.
        /// </summary>
        TagManage,
        /// <summary>
        /// Allows the role bearer to manage guild-specific settings.
        /// </summary>
        GuildManage
    }
}
