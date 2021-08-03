using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Doraemon.Services.GatewayEventHandlers;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Snipe")]
    [Description("Provides utilities for sniping deleted messages.")]
    public class SnipeModule : DiscordGuildModuleBase
    {
        [Command("snipe")]
        [Description("Snipes a deleted message.")]
        public DiscordCommandResult SnipeDeletedMessage(
            [Description("The channel to snipe, defaults to the current channel.")]
                ITextChannel channel = null)
        {
            if (channel == null)
            {
                var deletedMessage = MessageDeleted.DeletedMessages
                    .Find(x => x.ChannelId == Context.Channel.Id);
                if (deletedMessage == null)
                {
                    
                }
                else
                {
                    var user = Context.Guild.GetMember(deletedMessage.UserId);
                    var embed = new LocalEmbed()
                        .WithAuthor(user)
                        .WithDescription(deletedMessage.Content)
                        .WithTimestamp(deletedMessage.Time)
                        .WithColor(new Color(235, 0, 0));
                    return Response(embed);
                }

                return null;
            }

            //calling other channel's message
            var deletedMessage1 = MessageDeleted.DeletedMessages
                .Find(x => x.ChannelId == channel.Id);
            if (deletedMessage1 == null)
            {
                return Response("Nothing has been deleted yet.");
            }
            else
            {
                var user = Context.Guild.GetMember(deletedMessage1.UserId);
                var embed = new LocalEmbed()
                    .WithAuthor(user)
                    .WithDescription(deletedMessage1.Content)
                    .WithTimestamp(deletedMessage1.Time)
                    .WithColor(new Color(235, 0, 0));
                return Response(embed);
            }
        }
    }
}