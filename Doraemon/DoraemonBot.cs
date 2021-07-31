using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Doraemon
{
    public class DoraemonBot : DiscordBot
    {
        public DoraemonBot(IOptions<DiscordBotConfiguration> options, ILogger<DiscordBot> logger, IServiceProvider services, DiscordClient client) : base(options, logger, services, client)
        {
        }

        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            var warningReaction = new LocalEmoji("⚠️");
            await context.Message.AddReactionAsync(warningReaction);
        }
    }
}