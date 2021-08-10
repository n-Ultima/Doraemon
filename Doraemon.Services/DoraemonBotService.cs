using System;
using Disqord.Bot.Hosting;

namespace Doraemon.Services
{
    /// <summary>
    /// Implemented in microservices. Has an active instance of a <see cref="IServiceProvider"/>.
    /// </summary>
    public abstract class DoraemonBotService : DiscordBotService
    {
        internal protected IServiceProvider ServiceProvider;

        public DoraemonBotService(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider; 
    }
}