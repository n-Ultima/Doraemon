using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Disqord.Serialization.Json;
using Doraemon.Common;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class AuthenticateUser : DiscordBotService
    {
        public static DateTime DateTime;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private readonly AuthorizationService AuthorizationService;

        public AuthenticateUser(AuthorizationService authorizationService)
        {
            AuthorizationService = authorizationService;
        }

        public override int Priority => int.MaxValue;

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            DateTime = DateTime.Now;
            if (eventArgs.Channel == null) return;
            if (eventArgs.Message is not IUserMessage message) return;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var guildMember = guild.GetMember(message.Author.Id);
            if (guildMember == null)
                return;
            await AuthorizationService.AssignCurrentUserAsync(guildMember.Id, guildMember.RoleIds);
        }
    }
}