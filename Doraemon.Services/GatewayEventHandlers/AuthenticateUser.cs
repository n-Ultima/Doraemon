using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Doraemon.Common;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class AuthenticateUser : DoraemonEventService
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public AuthenticateUser(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)

        {
            
        }

        public override int Priority => int.MaxValue;

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return;
            if (eventArgs.Message is not IUserMessage message) return;
            if (eventArgs.Message.Author.IsBot) return;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var guildMember = eventArgs.Message.Author as IMember;
            await AuthorizationService.AssignCurrentUserAsync(guildMember.Id, guildMember.RoleIds);
        }
    }
}