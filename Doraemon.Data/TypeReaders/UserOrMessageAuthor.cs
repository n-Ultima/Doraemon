using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.TypeReaders
{
    /// <summary>
    /// Describes a <see cref="IUser"/>, or the author of a <see cref="IMessage"/>.  
    /// </summary>
    public class UserOrMessageAuthor
    {
        public UserOrMessageAuthor(ulong userId)
        {
            UserId = userId;
        }

        public UserOrMessageAuthor(ulong userId, ulong messageChannelId, ulong messageId)
        {
            UserId = userId;
            MessageChannelId = messageChannelId;
            MessageId = messageId;
        }

        public ulong UserId { get; }

        public ulong? MessageChannelId { get; }

        public ulong? MessageId { get; }
    }

    public class UserOrMessageAuthorEntityTypeReader : UserTypeReader<IGuildUser>
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            input = input.Trim(',', '.');

            var baseResult = await base.ReadAsync(context, input, services);

            if (baseResult.IsSuccess)
            {
                return TypeReaderResult.FromSuccess(new UserOrMessageAuthor(((IUser)baseResult.BestMatch).Id));
            }

            if (MentionUtils.TryParseUser(input, out var userId))
            {
                return SnowflakeUtilities.IsValidSnowflake(userId)
                    ? GetUserResult(userId)
                    : GetInvalidSnowflakeResult();
            }

            // The base class covers users that are in the guild, and the previous condition covers mentioning users that are not in the guild.
            // At this point, it's one of the following:
            //   - A snowflake for a message in the guild.
            //   - A snowflake for a user not in the guild.
            //   - Something we can't handle.
            if (ulong.TryParse(input, out var snowflake))
            {
                if (!SnowflakeUtilities.IsValidSnowflake(snowflake))
                {
                    return GetInvalidSnowflakeResult();
                }


                if (await FindMessageAsync(context.Guild.Id, snowflake, services) is { } message)
                {
                    return GetMessageAuthorResult(message.Author.Id, message.Channel.Id, message.Id);
                }

                // At this point, our best guess is that the snowflake is for a user who is not in the guild.
                return GetUserResult(snowflake);
            }

            return GetBadInputResult();
        }

        public async Task<IMessage> FindMessageAsync(ulong guildId, ulong messageId, IServiceProvider services)
        {
            var client = services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(guildId);
            var selfUser = guild.GetUser(client.CurrentUser.Id);
            var channels = guild.TextChannels.AsEnumerable();

            foreach (var channel in channels)
            {
                var message = selfUser.GetPermissions(channel).ReadMessageHistory
                    ? await channel.GetMessageAsync(messageId)
                    : channel.GetCachedMessage(messageId);
                if (message is { })
                    return message;
            }

            return null;
        }
        private static TypeReaderResult GetUserResult(ulong userId)
            => TypeReaderResult.FromSuccess(new UserOrMessageAuthor(userId));

        private static TypeReaderResult GetMessageAuthorResult(ulong userId, ulong messageChannelId, ulong messageId)
            => TypeReaderResult.FromSuccess(new UserOrMessageAuthor(userId, messageChannelId, messageId));

        private static TypeReaderResult GetInvalidSnowflakeResult()
            => TypeReaderResult.FromError(CommandError.ParseFailed, "Snowflake was almost certainly invalid.");

        private static TypeReaderResult GetBadInputResult()
            => TypeReaderResult.FromError(CommandError.ParseFailed, "Could not find a user or message.");
    }
}