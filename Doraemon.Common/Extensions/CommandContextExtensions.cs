using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Doraemon.Common.Extensions
{
    public static class CommandContextExtension
    {
        public static Emoji Success = new Emoji("✅");
        public static async Task AddConfirmationAsync(this ICommandContext context)
        {
            if (!(context.Channel is IGuildChannel guildChannel))
            {
                return;
            }
            var currentUser = await context.Guild.GetCurrentUserAsync();
            var permissions = currentUser.GetPermissions(guildChannel);
            if (!permissions.AddReactions)
            {
                await context.Channel.SendMessageAsync("I was unable to add the ✅ reaction to your message due to a permission error.");
                return;
            }
            await context.Message.AddReactionAsync(Success);
        }
    }
}
