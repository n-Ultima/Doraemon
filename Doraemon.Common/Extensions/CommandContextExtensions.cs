using System;
using System.Linq;
using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

namespace Doraemon.Common.Extensions
{
    public static class CommandContextExtension
    {
        public static LocalEmoji Success = new("✅");
        private static readonly LocalEmoji _xEmoji = new("❌");
        private const int _confirmationTimeoutSeconds = 10;

        public static async Task AddConfirmationAsync(this IUserMessage message, CachedMessageGuildChannel channel)
        {
            if (channel != null)
            {
                var guild = channel.Client.GetGuild(channel.GuildId);
                var currentUser = guild.GetMember(guild.Client.CurrentUser.Id);
                var permissions = currentUser.GetPermissions(channel);
                if(!permissions.AddReactions)
                {
                    await channel.SendMessageAsync(new LocalMessage()
                        .WithContent($"I was unable to add the ✅ to your message due to the `Add Reactions` not being allowed to me."));
                    return;
                }
            }
            await message.AddReactionAsync(Success);
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