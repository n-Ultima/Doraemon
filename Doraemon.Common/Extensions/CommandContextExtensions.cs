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

        public static async Task AddConfirmationAsync(this IUserMessage message, CachedGuildChannel channel)
        {
            if (channel != null)
            {
                var guild = channel.Client.GetGuild(channel.GuildId);
                var currentUser = guild.GetMember(guild.Client.CurrentUser.Id);
                var permissions = currentUser.GetPermissions(channel);
                if (!permissions.AddReactions)
                {
                    await (channel as ITextChannel).SendMessageAsync(new LocalMessage()
                        .WithContent($"I was unable to add the ✅ to your message due to the `Add Reactions` not being allowed to me."));
                    return;
                }
            }

            await message.AddReactionAsync(Success);
        }

        public static async Task AddConfirmationAsync(this DiscordGuildCommandContext context)
        {
            var guild = context.Guild;
            var currentUser = guild.GetMember(guild.Client.CurrentUser.Id);
            var channel = context.Channel;
            var permissions = currentUser.GetPermissions(channel);
            if (!permissions.AddReactions)
            {
                await (channel as ITextChannel).SendMessageAsync(new LocalMessage()
                    .WithContent($"I was unable to add the ✅ to your message due to the `Add Reactions` not being allowed to me."));
                return;
            }
            await context.Message.AddReactionAsync(Success);

        }
    }
}