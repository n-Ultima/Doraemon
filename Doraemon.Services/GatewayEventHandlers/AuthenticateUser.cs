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
    public class AuthenticateUser : DiscordBotService
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private readonly AuthorizationService AuthorizationService;
        public AuthenticateUser(AuthorizationService authorizationService)
        {
            AuthorizationService = authorizationService;
        }

        public override int Priority => int.MaxValue - 1;

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return;
            if (eventArgs.Message is not IUserMessage message) return;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var guildMember = eventArgs.Message.Author as IMember;
            await AuthorizationService.AssignCurrentUserAsync(guildMember.Id, guildMember.RoleIds);
        }
    }
}