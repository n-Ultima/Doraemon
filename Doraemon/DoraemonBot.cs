using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Data.TypeReaders;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using RestSharp.Extensions;
using Serilog;

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

            if (result is CommandExecutionFailedResult commandFailedException)
            {
                await context.Message.AddReactionAsync(warningReaction);
                await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessage().WithContent($"Error: {commandFailedException.Exception.Message}"));
            }

            if (result is OverloadsFailedResult failedOverloadResult)
            {
                await context.Message.AddReactionAsync(warningReaction);
                await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessage().WithContent($"Error: {failedOverloadResult.FailedOverloads.Values.Humanize()}"));
            }

            if (result is ChecksFailedResult checksFailedResult)
            {
                await context.Message.AddReactionAsync(warningReaction);
                await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessage().WithContent($"Error: {checksFailedResult.FailedChecks.Humanize()}\nChecks: {checksFailedResult.FailedChecks.Select(x => x.Check)}"));
            }

            if (result is ArgumentParseFailedResult parseFailedResult)
            {
                await context.Message.AddReactionAsync(warningReaction);
                await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessage()
                    .WithContent($"Error: {parseFailedResult.ParserResult}"));
            }
        }

        protected override ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Commands.AddTypeParser(new TimeSpanTypeReader());
            return base.AddTypeParsersAsync(cancellationToken);
        }
        
    }
}