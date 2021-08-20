using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;

namespace Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers
{
    public class GuildUserTrackingBehavior : DoraemonEventService
    {
        private readonly GuildUserService _guildUserService;

        public GuildUserTrackingBehavior(AuthorizationService authorizationService, InfractionService infractionService, GuildUserService guildUserService)
            : base(authorizationService, infractionService)
        {
            _guildUserService = guildUserService;
        }

        public override int Priority => int.MaxValue - 4;

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Message is not IUserMessage message) return;
            if (message.Author.IsBot) return;
            var userToUpdate = await _guildUserService.FetchGuildUserAsync(message.Author.Id);
            if (userToUpdate is null)
            {
                await _guildUserService.CreateGuildUserAsync(message.Author.Id, message.Author.Name,
                    message.Author.Discriminator, false);
            }
            else
            {
                if (userToUpdate.Username != message.Author.Name)
                    await _guildUserService.UpdateGuildUserAsync(message.Author.Id, message.Author.Name, null, null);
                if (userToUpdate.Discriminator != message.Author.Discriminator)
                    await _guildUserService.UpdateGuildUserAsync(message.Author.Id, null, message.Author.Discriminator, null);
            }
        }
    }
}