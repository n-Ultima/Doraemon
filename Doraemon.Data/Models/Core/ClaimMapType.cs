namespace Doraemon.Data.Models.Core
{
    public enum ClaimMapType
    {
        /// <summary>
        ///     Allows the claim bearer to use the snipe command.
        /// </summary>
        UseSnipe,
        
        /// <summary>
        ///     Allows the claim bearer to read on-going promotions.
        /// </summary>
        PromotionRead,

        /// <summary>
        ///     Allows the claim bearer to comment on an ongoing campaign.
        /// </summary>
        PromotionComment,
        
        /// <summary>
        ///     Allows the claim bearer to start campaigns.
        /// </summary>
        PromotionStart,

        /// <summary>
        ///     Allows the claim bearer to accept or deny campaigns.
        /// </summary>
        PromotionManage,

        /// <summary>
        ///     Allows the claim bearer to create tags.
        /// </summary>
        CreateTag,
        
        /// <summary>
        ///     Allows the claim bearer to create, edit, and delete tags that they own.
        /// </summary>
        MaintainOwnedTag,
        
        /// <summary>
        ///     Allows the claim bearer to edit and delete tags that they do not own.
        /// </summary>
        MaintainOtherUserTag,

        /// <summary>
        ///     Allows the claim bearer to bypass auto-moderation.
        /// </summary>
        BypassAutoModeration,

        /// <summary>
        ///     Allows the role bearer to respond to modmail threads.
        /// </summary>
        ModmailRespond,
        
        /// <summary>
        /// Allows the claim bearer to view snippets.
        /// </summary>
        ModmailSnippetView,
        
        /// <summary>
        ///     Allows the role bearer to manage snippets.
        /// </summary>
        ModmailSnippetManage,
        
        /// <summary>
        ///     Allows the claim bearer to block users from opening modmail threads.
        /// </summary>
        ModmailBlock,
        
        /// <summary>
        ///     Allows the claim bearer to read infraction history.
        /// </summary>
        InfractionView,

        /// <summary>
        ///     Allows the claim bearer to create infractions.
        /// </summary>
        InfractionCreate,

        /// <summary>
        ///     Allows the claim bearer to delete infractions.
        /// </summary>
        InfractionDelete,

        /// <summary>
        ///     Allows the claim bearer to update infractions.
        /// </summary>
        InfractionUpdate,
        
        /// <summary>
        /// Allows the claim bearer to manage pingroles.
        /// </summary>
        GuildPingRoleManage,
        
        /// <summary>
        ///     Allows the claim bearer to manage the raidmode setting for the guild.
        /// </summary>
        GuildRaidModeManage,
        
        /// <summary>
        ///     Allows the role bearer to manage whitelisted invites.
        /// </summary>
        GuildInviteWhitelistManage,
        
        /// <summary>
        ///     Allows the claim bearer to manage the guild's punishment escalations.
        /// </summary>
        GuildPunishmentEscalationManage,

        /// <summary>
        ///     Allows the claim bearer to edit role claims to other users and roles.
        /// </summary>
        AuthorizationClaimManage
    }
}