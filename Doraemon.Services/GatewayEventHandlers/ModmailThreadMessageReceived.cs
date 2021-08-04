using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Services.Modmail;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class ModmailThreadMessageReceivedHandler : DoraemonEventService
    {
        private readonly ModmailTicketService _modmailTicketService;
        private readonly GuildUserService _guildUserService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        
        public ModmailThreadMessageReceivedHandler(AuthorizationService authorizationService, InfractionService infractionService, ModmailTicketService modmailTicketService, GuildUserService guildUserService)
            : base(authorizationService, infractionService)
        {
            _modmailTicketService = modmailTicketService;
            _guildUserService = guildUserService;
        }

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return;
            if (eventArgs.Message is not IUserMessage message) return;
            if (message.Author.IsBot) return;
            var modmailGuild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var modmailCategory = modmailGuild.GetChannel(DoraemonConfig.ModmailCategoryId) as ICategoryChannel;
            var originatingChannel = await message.FetchChannelAsync() as ITextChannel;
            if (originatingChannel == null) return;
            if (!originatingChannel.CategoryId.HasValue) return;
            if (originatingChannel.CategoryId.Value != DoraemonConfig.ModmailCategoryId) return;
            var guildUser = message.Author as IMember;
            var guildUserHighestRole = guildUser.GetRoles()
                .OrderByDescending(x => x.Value.Position)
                .Select(x => x.Value.Name)
                .First();
            if (originatingChannel.CategoryId.Value != DoraemonConfig.ModmailCategoryId) return;
            var ongoingModmailThread = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(message.ChannelId);
            if (ongoingModmailThread == null) return;
            var dmChannel = ongoingModmailThread.DmChannelId;
            if (message.Content.StartsWith(DoraemonConfig.Prefix)) return; // Ignore commands
            var embed = new LocalEmbed()
                .WithColor(Color.Green)
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(guildUserHighestRole);
            if (message.Attachments.Any())
            {
                embed.WithImageUrl(message.Attachments.ElementAt(0).Url);
            }

            await Bot.SendMessageAsync(dmChannel, new LocalMessage().WithEmbeds(embed));
            await message.AddConfirmationAsync(originatingChannel as CachedGuildChannel);
            await _modmailTicketService.AddMessageToModmailTicketAsync(ongoingModmailThread.Id, message.Author.Id, $"(Staff){message.Author.Tag} - {message.Content}\n");
        }
    }
}