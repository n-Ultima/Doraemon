using Disqord.Bot.Hosting;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.GatewayEventHandlers
{
    /// <summary>
    /// Used for handling gateway events. Contains an instance of the <see cref="AuthorizationService"/> and <see cref="InfractionService"/>.
    /// </summary>
    public abstract class DoraemonEventService : DiscordBotService
    {
        internal protected AuthorizationService AuthorizationService { get; set; }

        internal protected InfractionService InfractionService { get; set; }
        public DoraemonEventService(AuthorizationService authorizationService, InfractionService infractionService)
        {
            AuthorizationService = authorizationService;
            InfractionService = infractionService;
        }
    }
}