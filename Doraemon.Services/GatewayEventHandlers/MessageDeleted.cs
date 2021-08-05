using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class MessageDeleted : DoraemonEventService
    {
        public static List<DeletedMessage> DeletedMessages = new();
        public DoraemonConfiguration DorameonConfig { get; private set; } = new();
        public MessageDeleted(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {
        }

        protected override async ValueTask OnMessageDeleted(MessageDeletedEventArgs eventArgs)
        {
            if (eventArgs.Message == null) return;
            if (string.IsNullOrEmpty(eventArgs.Message.Content)) return;
            var message = eventArgs.Message;

            var listedMessage = DeletedMessages
                .Find(x => x.ChannelId == message.ChannelId);
            if (listedMessage == null) //check if the channel has a deleted message listed
            {
                DeletedMessages.Add(new DeletedMessage
                {
                    Content = message.Content, UserId = message.Author.Id, ChannelId = message.ChannelId,
                    Time = message.CreatedAt().LocalDateTime, DeleteTime = DateTime.Now + TimeSpan.FromMinutes(30)
                }); //when the method checks the list again, it will delete messages older than 30 minutes
            }
            else
            {
                DeletedMessages.Remove(listedMessage);
                DeletedMessages.Add(new DeletedMessage
                {
                    Content = message.Content, UserId = message.Author.Id, ChannelId = message.ChannelId,
                    Time = message.CreatedAt().LocalDateTime, DeleteTime = DateTime.Now + TimeSpan.FromMinutes(30)
                });
            }

            var Remove = new List<DeletedMessage>(); //filter out the list below
            foreach (var deletedMessage in DeletedMessages)
            {
                if (DateTime.Now > deletedMessage.DeleteTime)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }

                IGuild messageGuild = null;
                var guilds = Bot.GetGuilds().Values.ToList();
                foreach (var guild in guilds)
                {
                    var currentChannel = guild.GetChannels().ToList()
                        .Find(x => x.Value.Id == deletedMessage.ChannelId);
                    if (currentChannel.Value != null) 
                        messageGuild = Bot.GetGuild(currentChannel.Value.GuildId);
                }

                if (messageGuild == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }

                var messageContent = deletedMessage.Content;
                if (messageContent == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }

                var messageChannel = messageGuild.GetChannel(deletedMessage.ChannelId);
                if (messageChannel == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }

                var messageUser = messageGuild.GetMember(deletedMessage.UserId);
                if (messageUser == null) Remove.Add(deletedMessage);
            }

            DeletedMessages = DeletedMessages.Except(Remove).ToList();
            DeletedMessages = DeletedMessages.Except(Remove).ToList();

            var embed = new LocalEmbed()
                .WithColor(DColor.DarkRed)
                .WithDescription($"Message deleted in {Mention.Channel(message.ChannelId)}\nContent: {message.Content}")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithAuthor(message.Author);
            await Bot.SendMessageAsync(DorameonConfig.LogConfiguration.MessageLogChannelId, new LocalMessage().WithEmbeds(embed));
        }
    }
}