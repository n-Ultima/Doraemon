namespace Doraemon.Data.Models.Core
{
    public enum ClaimMapType
    {
        /// <summary>
        ///     Allows the role bearer to read infraction history.
        /// </summary>
        InfractionView,

        /// <summary>
        ///     Allows the role bearer to create infractions.
        /// </summary>
        InfractionCreate,

        /// <summary>
        ///     Allows the role bearer to delete infractions.
        /// </summary>
        InfractionDelete,

        /// <summary>
        ///     Allows the role bearer to update infractions.
        /// </summary>
        InfractionUpdate,

        /// <summary>
        ///     Allows the role bearer to manage modmail threads.
        /// </summary>
        ModmailManage,

        /// <summary>
        ///     Allows the role bearer to read on-going promotions.
        /// </summary>
        PromotionRead,

        /// <summary>
        ///     Allows the role bearer to start campaigns.
        /// </summary>
        PromotionStart,

        /// <summary>
        ///     Allows the role bearer to accept or deny campaigns.
        /// </summary>
        PromotionManage,

        /// <summary>
        ///     Allows the role bearer to comment on an ongoing campaign.
        /// </summary>
        PromotionComment,

        /// <summary>
        ///     Allows the role bearer to create, edit, and delete tags that they own.
        /// </summary>
        TagManage,

        /// <summary>
        ///     Allows the role bearer to manage guild-specific settings.
        /// </summary>
        GuildManage,

        /// <summary>
        ///     Allows the role bearer to edit role claims to other roles.
        /// </summary>
        AuthorizationManage
    }
}