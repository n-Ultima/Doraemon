using System;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Data.TypeReaders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using RestSharp.Extensions;

namespace Doraemon
{
    public class DoraemonBot : DiscordBot
    {
        public DoraemonBot(IOptions<DiscordBotConfiguration> options, ILogger<DiscordBot> logger, IServiceProvider services, DiscordClient client) : base(options, logger, services, client)
        {
        }


        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            var exception = result as CommandExecutionFailedResult;
            var actualMessage = exception.Exception.Message;
            var warningReaction = new LocalEmoji("⚠️");
            await context.Message.AddReactionAsync(warningReaction);
            await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessage().WithContent($"Error: {actualMessage}"));
        }

        protected override ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Commands.AddTypeParser(new TimeSpanTypeReader());
            return base.AddTypeParsersAsync(cancellationToken);
        }
        
    }
}