namespace Doraemon.Data.Models
{
    /// <summary>
    ///     The type of the infraction.
    /// </summary>
    public enum InfractionType
    {
        /// <summary>
        /// Represents a ban, kicking the user from the guild and preventing re-entry.
        /// </summary>
        Ban,
        /// <summary>
        /// Represents a mute, silencing the user and preventing them from chatting in text channels.
        /// </summary>
        Mute,
        /// <summary>
        /// Represents a formal warning given to a user.
        /// </summary>
        Warn,
        /// <summary>
        /// Represents a note, mainly used to keep tabs on users between moderators.
        /// </summary>
        Note
    }
}