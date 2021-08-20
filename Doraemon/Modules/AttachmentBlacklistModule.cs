using System.Linq;
using System.Text;
using Disqord.Bot;
using Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Attachment Blacklist")]
    [Description("Retrieves information on blacklisted attachments.")]
    public class AttachmentBlacklistModule : DiscordGuildModuleBase
    {
        [Command("attachment-blacklists")]
        [Description("Lists all blacklisted attachment types.")]
        public DiscordCommandResult ListAllBlacklistedAttachmentTypes()
        {
            var blacklistBuilder = new StringBuilder()
                .AppendLine($"**Blacklisted File Extensions:**")
                .Append("```")
                .AppendJoin(", ", AutoModerationHandler.BlacklistedExtensions.OrderBy(d => d))
                .Append("```");
            return Response(blacklistBuilder.ToString());
        }
    }
}