using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord.Bot;
using Doraemon.Services.GatewayEventHandlers;
using Microsoft.SqlServer.Server;
using Qmmands;
using RestSharp.Extensions;

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
                .AppendJoin(", ", AutoModeration.BlacklistedExtensions.OrderBy(d => d))
                .Append("```");
            return Response(blacklistBuilder.ToString());
        }
    }
}