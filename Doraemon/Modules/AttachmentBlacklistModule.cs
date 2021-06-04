using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Data.Events.MessageReceivedHandlers;

namespace Doraemon.Modules
{
    [Name("Attachment Blacklist")]
    [Summary("Retrieves information on blacklisted attachments.")]
    public class AttachmentBlacklistModule : ModuleBase
    {
        [Command("attachment blacklists")]
        [Summary("Lists all blacklisted attachment types.")]
        public async Task ListBlacklistedAttachmentTypesAsync()
        {
            var blacklistBuilder = new StringBuilder()
                .AppendLine($"{Format.Bold("Blacklisted Extensions")}:")
                .Append("```")
                .AppendJoin(", ", AutoModeration.BlacklistedExtensions.OrderBy(d => d))
                .Append("```");
            await ReplyAsync(blacklistBuilder.ToString());
        }
    }
}
