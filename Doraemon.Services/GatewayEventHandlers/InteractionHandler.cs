using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class InteractionHandler : DoraemonEventService
    {
        public InteractionHandler(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {}


        protected override async ValueTask OnInteractionReceived(InteractionReceivedEventArgs eventArgs)
        {
            if (eventArgs.Interaction is ITextCommandInteraction textCommandInteraction)
            {
                switch (textCommandInteraction.CommandName)
                {
                    case "attachment-blacklists":
                    {
                        var blacklistBuilder = new StringBuilder()
                            .AppendLine($"**Blacklisted File Extensions:**")
                            .Append("```")
                            .AppendJoin(", ", AutoModerationHandler.BlacklistedExtensions.OrderBy(d => d))
                            .Append("```");
                        await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                            .WithIsEphemeral()
                            .WithContent(blacklistBuilder.ToString()));
                        break;
                    }
                }
            }
        }
    }
}