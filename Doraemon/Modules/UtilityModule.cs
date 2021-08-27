using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Utility")]
    [Description("Provides commands more focused on utility than anything else.")]
    public class UtilityModule : DoraemonGuildModuleBase
    {
        [Command("snowflake")]
        [Description("Provides information on the snowflake provided.")]
        public DiscordCommandResult CheckSnoflake(
            [Description("The snowflake to query for information on.")]
                Snowflake snowflake)
        {
            var dateCreatedAt = snowflake.CreatedAt;
            var embed = new LocalEmbed()
                .WithTitle($"❄ Snowflake Information")
                .AddField("Created At", snowflake.CreatedAt.ToString("f"))
                .WithColor(DColor.Blue);
            return Response(embed);
        }

        [Command("message", "messageinfo")]
        [Description("Shows information about a message.")]
        public async Task<DiscordCommandResult> GetMessageInfoAsync(
            [Description("The ID of the message.")]
                Snowflake messageId)
        {
            IMessage message;
            message = await Bot.FetchMessageAsync(Context.ChannelId, messageId);
            if (message == null)
            {
                foreach (var channel in Context.Guild.GetChannels().Select(x => x.Value))
                {
                    if (channel is not ITextChannel textChannel) return null;
                    message = await textChannel.FetchMessageAsync(messageId);
                    if (message == null)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (message == null)
                {
                    throw new Exception($"The message ID provided doesn't exist, or the bot can't find it.");
                }
            }

            var messageChannel = await message.FetchChannelAsync();
            var embed = new LocalEmbed()
                .WithTitle($"Information for message Id {message.Id}")
                .AddField("Content", message.Content)
                .AddField("Channel Sent In", $"{Mention.Channel(messageChannel.Id)}")
                .AddField("Author", $"{Mention.User(message.Author)}")
                .WithColor(DColor.Gold);
            return Response(embed);
        }

        [Command("jumbo", "jumbofy")]
        [Description("Jumbofies the given emoji.")]
        public DiscordCommandResult Jumbofy(
            [Description("The emoji to jumbofy.")]
                ICustomEmoji emoji)
        {
            var emojiUrl = emoji.GetUrl();
            if (emojiUrl == null)
                throw new Exception($"I don't recongnize that emoji.");
            return Response(emojiUrl);
        }
    }
}