using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Extensions
{
    public static class CommandContextExtension
    {
        public static Emoji Success = new("✅");

        public static async Task AddConfirmationAsync(this ICommandContext context)
        {
            if (!(context.Channel is IGuildChannel guildChannel)) return;
            var currentUser = await context.Guild.GetCurrentUserAsync();
            var permissions = currentUser.GetPermissions(guildChannel);
            if (!permissions.AddReactions)
            {
                await context.Channel.SendMessageAsync(
                    "I was unable to add the ✅ reaction to your message due to a permission error.");
                return;
            }

            await context.Message.AddReactionAsync(Success);
        }
    }

    public static class MessageContextExtension
    {
        public static Emoji Success = new("✅");

        public static async Task AddConfirmationAsync(this SocketMessage arg)
        {
            await arg.AddReactionAsync(Success);
        }
    }
}