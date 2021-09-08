using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        public DiscordCommandResult CheckSnowflake(
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

        [Command("steal")]
        [Description("Steals the emoji provied and adds it to your server.")]
        public async Task<DiscordCommandResult> StealEmojiAsync(
            [Description("The emoji to steal.")]
                ICustomEmoji emoji)
        {
            var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(emoji.GetUrl(size: 2048));
            await Context.Guild.CreateEmojiAsync(emoji.Name, stream);
            return Confirmation();
        }

        [Command("steal")]
        [Description("Steals an emoji via the url to the given emoji.")]
        public async Task<DiscordCommandResult> StealEmojiAsync(
            [Description("The link to the emoji.")]
                string url,
            [Description("The name of the emoji.")]
                string name)
        {
            var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(url);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            await Context.Guild.CreateEmojiAsync(name, ms);
            return Confirmation();
        }
    }
}