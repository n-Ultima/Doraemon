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


        protected override LocalMessage FormatFailureMessage(DiscordCommandContext context, FailedResult result)
        {
            if (result is CommandExecutionFailedResult commandFailedResult)
            {
                return new LocalMessage()
                    .WithContent($"Error: {commandFailedResult.Exception.Message}");
            }

            if (result is OverloadsFailedResult overloadsFailedResult)
            {
                return new LocalMessage()
                    .WithContent($"Error: No matching overloads were found for the command provided.");
            }

            if (result is ArgumentParseFailedResult argumentParseFailedResult)
            {
                return new LocalMessage()
                    .WithContent($"Error: {argumentParseFailedResult.ParserResult}");
            }

            if (result is CommandNotFoundResult commandNotFoundResult)
            {
                return new LocalMessage()
                    .WithContent($"Error: Command not found.");
            }
            Log.Logger.Error($"{result.FailureReason}");
            return new LocalMessage()
                .WithContent("There was an error just now, please check the inner exception for more details.");
        }

        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            var warningReaction = new LocalEmoji("⚠️");
            await context.Message.AddReactionAsync(warningReaction);
            await base.HandleFailedResultAsync(context, result);
        }

        
        protected override ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Commands.AddTypeParser(new TimeSpanTypeReader());
            return base.AddTypeParsersAsync(cancellationToken);
        }
        
    }
}