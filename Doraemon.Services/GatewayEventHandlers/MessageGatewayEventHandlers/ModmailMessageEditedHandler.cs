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
            if (e.NewMessage == null) return;
            if (e.NewMessage is not IUserMessage message) return;
            if (message.Author.IsBot) return;
            var modmailGuild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var modmailThread = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(message.ChannelId);
            // Came from DM
            if (e.GuildId == null)
            {
                modmailThread = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(message.ChannelId);
                if (modmailThread == null)
                    return;
                var localEmbed = new LocalEmbed()
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithColor(DColor.Gold)
                    .WithTitle($"Message Edited")
                    .WithDescription($"**Before:**: {e.OldMessage.Content ?? "Cache failure."}\n**After:** {message.Content ?? "Cache failure"}")
                    .WithAuthor(message.Author)
                    .WithFooter($"Edited Message Id: {message.Id}");
                if (message.Attachments.Any())
                {
                    localEmbed.WithImageUrl(message.Attachments[0].Url);
                }

                var modmailThreadChannel = modmailGuild.GetChannel(modmailThread.ModmailChannelId) as ITextChannel;
                await modmailThreadChannel.SendMessageAsync(new LocalMessage().WithEmbeds(localEmbed));
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailThread.Id, message.Author.Id, $"Message edited by {message.Author.Tag}\nBefore: {e.OldMessage.Content}\nAfter: {e.NewMessage.Content}");
                
            }
        }
    }
}