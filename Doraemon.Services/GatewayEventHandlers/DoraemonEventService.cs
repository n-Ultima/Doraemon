using System;
using Disqord.Bot.Hosting;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.GatewayEventHandlers
{
    /// <summary>
    /// Used for handling gateway events and background services. Contains an instace of a <see cref="IServiceProvider"/>.
    /// </summary>
    public abstract class DoraemonEventService : DiscordBotService
    {
        internal protected AuthorizationService AuthorizationService;
        internal protected InfractionService InfractionService;
        public DoraemonEventService(AuthorizationService authorizationService, InfractionService infractionService)
        {
            AuthorizationService = authorizationService;
            InfractionService = infractionService;
        }
    }
}