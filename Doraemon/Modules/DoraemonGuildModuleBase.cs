using Disqord;
using Disqord.Bot;

namespace Doraemon.Modules
{
    /// <summary>
    /// Represents a <see cref="DiscordGuildModuleBase"/> with custom <see cref="DiscordCommandResult"/>s.
    /// </summary>
    public class DoraemonGuildModuleBase : DiscordGuildModuleBase
    {
        /// <summary>
        /// Returns a <see cref="DiscordCommandResult"/> that adds the ✅ to the message.
        /// </summary>
        /// <returns>A <see cref="DiscordCommandResult"/> representing success of a command.</returns>
        protected DiscordCommandResult Confirmation()
        {
            return Reaction(new LocalEmoji("✅"));
        }
    }
}