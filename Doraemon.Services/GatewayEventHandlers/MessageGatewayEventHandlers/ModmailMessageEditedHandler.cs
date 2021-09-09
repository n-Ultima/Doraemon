using System;
using System.CodeDom;
using System.Formats.Asn1;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Api;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Services.Modmail;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers
{
    public class ModmailMessageEditedHandler : DoraemonEventService
    {
        private readonly ModmailTicketService _modmailTicketService;
        private readonly IServiceProvider _serviceProvider;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public ModmailMessageEditedHandler(AuthorizationService authorizationService, InfractionService infractionService, ModmailTicketService modmailTicketService, IServiceProvider serviceProvider)
            : base(authorizationService, infractionService)
        {
            _modmailTicketService = modmailTicketService;
            _serviceProvider = serviceProvider;

        }
        // We will use this method to handle messages edited in modmail threads only. We pray to lord Duck that the cache does cache things
        // please work cache
        protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
        {
            var modmailThread = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(e.ChannelId);
            var newMessage = e.Model.Content.Value;
            var messages = await _modmailTicketService.FetchModmailMessagesAsync(modmailThread.Id);
            var oldMessage = messages
                .Where(x => x.MessageId == e.MessageId)
                .FirstOrDefault();
            if (oldMessage == null)
                return;
            if (e.Model.Author.Value.Bot.HasValue) return;
            var modmailGuild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            // Came from DM
            if (e.GuildId == null)
            {
                modmailThread = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(e.ChannelId);
                if (modmailThread == null)
                    return;
                var modmailThreadChannel = modmailGuild.GetChannel(modmailThread.ModmailChannelId) as ITextChannel;
                await modmailThreadChannel.SendMessageAsync(new LocalMessage().WithContent($"**Message Edited**\n`B`: {oldMessage.Content}\n`N`: {newMessage}"));
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailThread.Id, e.Model.Author.Value.Id, e.MessageId, $"Message edited by {e.Model.Author.Value.Username + e.Model.Author.Value.Discriminator}\nBefore: {oldMessage.Content}\nAfter: {newMessage}");
                
            }
        }
    }
}