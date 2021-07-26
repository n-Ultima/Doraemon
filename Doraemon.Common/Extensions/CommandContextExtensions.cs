using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Extensions
{
    public static class CommandContextExtension
    {
        public static Emoji Success = new("✅");
        private static readonly Emoji _xEmoji = new Emoji("❌");
        private const int _confirmationTimeoutSeconds = 10;

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

        public static async Task<bool> GetUserConfirmationAsync(this ICommandContext context, string mainMessage)
        {
            if (!(context.Channel is IGuildChannel guildChannel))
            {
                return false;
            }

            var currentUser = await context.Guild.GetCurrentUserAsync();
            var permissions = currentUser.GetPermissions(guildChannel);

            if (!permissions.AddReactions)
            {
                throw new InvalidOperationException("Unable to get user confirmation, because the AddReactions permission is denied.");
            }

            if (!mainMessage.EndsWith(Environment.NewLine))
                mainMessage += Environment.NewLine;

            var confirmationMessage = await context.Channel.SendMessageAsync(mainMessage +
                                                                             $"React with {Success} or {_xEmoji} in the next {_confirmationTimeoutSeconds} seconds to finalize or cancel the operation.");

            await confirmationMessage.AddReactionAsync(Success);
            await confirmationMessage.AddReactionAsync(_xEmoji);

            for (var i = 0; i < _confirmationTimeoutSeconds; i++)
            {
                await Task.Delay(1000);

                var denyingUsers = await confirmationMessage.GetReactionUsersAsync(_xEmoji, int.MaxValue).FlattenAsync();
                if (denyingUsers.Any(u => u.Id == context.User.Id))
                {
                    await RemoveReactionsAndUpdateMessage("Cancellation was successfully received. Cancelling the operation.");
                    return false;
                }

                var confirmingUsers = await confirmationMessage.GetReactionUsersAsync(Success, int.MaxValue).FlattenAsync();
                if (confirmingUsers.Any(u => u.Id == context.User.Id))
                {
                    await RemoveReactionsAndUpdateMessage("Confirmation was successfully received. Performing the operation.");
                    return true;
                }
            }

            await RemoveReactionsAndUpdateMessage("Confirmation was not received. Cancelling the operation.");
            return false;

            async Task RemoveReactionsAndUpdateMessage(string bottomMessage)
            {
                await confirmationMessage.RemoveAllReactionsAsync();
                await confirmationMessage.ModifyAsync(m => m.Content = mainMessage + bottomMessage);
            }
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