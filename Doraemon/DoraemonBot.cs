using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Sharding;
using Disqord.Rest;
using Disqord.Sharding;
using Doraemon.Common;
using Doraemon.Data.TypeReaders;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using RestSharp.Extensions;
using Serilog;

namespace Doraemon
{
    public class DoraemonBot : DiscordBotSharder
    {
        public DoraemonBot(IOptions<DiscordBotSharderConfiguration> options, ILogger<DiscordBotSharder> logger, IServiceProvider services, DiscordClientSharder client) : base(options, logger, services, client)
        {
        }

        public DoraemonConfiguration DorameonConfig { get; private set; } = new();
        private static int warn = 0;
        

        protected override LocalMessage FormatFailureMessage(DiscordCommandContext context, FailedResult result)
        {
            if (result is OverloadsFailedResult overloadsFailedResult)
            {
                static string FormatParameter(Parameter parameter)
                {
                    string format;
                    if (parameter.IsMultiple)
                    {
                        format = "{0}[]";
                    }
                    else
                    {
                        format = parameter.IsRemainder
                            ? "{0}..."
                            : "{0}";
                        format = parameter.IsOptional
                            ? $"[{format}]"
                            : $"<{format}>";
                    }

                    return string.Format(format, parameter.Name);
                }

                var builder = new StringBuilder();
                builder.AppendLine($"The input given doesn't match any overloads.\nAvailable overloads:");
                foreach (var (overload, overloadResult) in overloadsFailedResult.FailedOverloads)
                {
                    var overloadReason = base.FormatFailureReason(context, overloadResult);
                    if (overloadReason == null)
                        continue;
                    builder.AppendLine($"`{DorameonConfig.Prefix}{overload.FullAliases[0]} {string.Join(' ', overload.Parameters.Select(FormatParameter))}`");
                }

                return new LocalMessage()
                    .WithContent(builder.ToString());
            }

            if (result is ArgumentParseFailedResult argumentParseFailedResult)
            {
                static string FormatParameter(Parameter parameter)
                {
                    string format;
                    if (parameter.IsMultiple)
                    {
                        format = "{0}[]";
                    }
                    else
                    {
                        format = parameter.IsRemainder
                            ? "{0}..."
                            : "{0}";
                        format = parameter.IsOptional
                            ? $"[{format}]"
                            : $"<{format}>";
                    }

                    return string.Format(format, parameter.Name);
                }

                var builder = new StringBuilder();
                var overloadReason = base.FormatFailureReason(context, argumentParseFailedResult);
                builder.AppendLine($"Command: `{DorameonConfig.Prefix}{argumentParseFailedResult.Command.FullAliases[0]} {string.Join(' ', argumentParseFailedResult.Command.Parameters.Select(FormatParameter))}`\n{overloadReason}");
                return new LocalMessage()
                    .WithContent(builder.ToString());
            }
            if (result is CommandExecutionFailedResult commandFailedResult)
            {
                if (commandFailedResult.Exception.Message == "Promotion Module needs settings not specified in config.json")
                {
                    warn++;
                    return new LocalMessage()
                            .WithContent("To use this module, you need to specify both PromotionRoleId and PromotionLogChannelId inside of config.json. This will only appear once.");
                }
                return new LocalMessage()
                    .WithContent($"Error: {commandFailedResult.Exception.Message}");
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
            if (result is CommandExecutionFailedResult commandExecutionFailedResult)
            {
                if (commandExecutionFailedResult.Exception.Message == "Promotion Module needs settings not specified in config.json")
                {
                    if (warn != 0)
                    {
                        return;
                    }
                }
            }
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