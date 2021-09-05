using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers;
using Doraemon.Services.Moderation;
using Doraemon.Services.Modmail;

namespace Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers
{
    public class PrivateMessageReceivedHandler : DoraemonEventService
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        private readonly GuildUserService _guildUserService;

        private readonly ModmailTicketService _modmailTicketService;
        
        public PrivateMessageReceivedHandler(AuthorizationService authorizationService, InfractionService infractionService, GuildUserService guildUserService, ModmailTicketService modmailTicketService)
            : base(authorizationService, infractionService)
        {
            _guildUserService = guildUserService;
            _modmailTicketService = modmailTicketService;
        }

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            // This means it's in a DM Channel, as all guild channels would be cached.
            if (eventArgs.Channel != null) return;
            if (eventArgs.Message is not IUserMessage message) return;
            if (message.Author.IsBot) return;
            var modmailAuthor = await _guildUserService.FetchGuildUserAsync(message.Author.Id.RawValue);
            if (modmailAuthor.IsModmailBlocked)
                return;
            var dmModmail = await _modmailTicketService.FetchModmailTicketAsync(message.Author.Id);
            var modMailGuild = Bot.GetGuild(DoraemonConfig.MainGuildId); // Get the guild defined in config.json
            var modmailLogChannel = modMailGuild.GetChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId) as ITextChannel;
            var modMailCategory = modMailGuild.GetChannel(DoraemonConfig.ModmailCategoryId) as ICategoryChannel;
            // If the user does not have an ongoing modmail thread.
            if (dmModmail == null)
            {
                var id = DatabaseUtilities.ProduceId();
                await Bot.SendMessageAsync(message.ChannelId, new LocalMessage()
                    .WithContent($"Thanks for contacting Staff! We'll get back to you as soon as possible."));
                var textChannel = await modMailGuild.CreateTextChannelAsync(message.Author.Tag, x =>
                {
                    x.CategoryId = modMailCategory.Id;
                    x.Topic = message.Author.Id.ToString();
                });
                var embed = new LocalEmbed()
                    .WithAuthor(message.Author)
                    .WithColor(DColor.Gold)
                    .WithDescription(message.Content)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithFooter($"Message ID: {message.Id}")
                    .WithFooter($"Ticket ID: {id}");
                if (message.Attachments.Any())
                {
                    embed.WithImageUrl(message.Attachments.ElementAt(0).Url);
                }
                await textChannel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
                await _modmailTicketService.CreateModmailTicketAsync(id, message.Author.Id, message.ChannelId, textChannel.Id);
                await _modmailTicketService.AddMessageToModmailTicketAsync(id, message.Author.Id, $"User: {message.Author.Tag} created a modmail thread with message: {message.Content}\nTicket Id: {id}\nDmChannelId: {message.ChannelId}\n\n");
                await message.AddConfirmationAsync(null);
            }
            // If the user already has an ongoing modmail thread.
            else
            {
                var ongoingModmail = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(message.ChannelId);
                var ongoingModmailThreadChannel = modMailGuild.GetChannel(ongoingModmail.ModmailChannelId) as ITextChannel;

                var embed = new LocalEmbed()
                    .WithAuthor(message.Author)
                    .WithColor(DColor.Gold)
                    .WithDescription(message.Content)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithFooter($"Message ID: {message.Id}");
                if (message.Attachments.Any())
                {
                    embed.WithImageUrl(message.Attachments.ElementAt(0).Url);
                }

                await ongoingModmailThreadChannel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
                await _modmailTicketService.AddMessageToModmailTicketAsync(ongoingModmail.Id, message.Author.Id, $"{message.Author.Tag} - {message.Content}\n");
                await message.AddConfirmationAsync(null);
            }
        }
    }
}