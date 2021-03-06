using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Doraemon.Common;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.GatewayEventHandlers.MessageGatewayEventHandlers
{
    public class DynamicSpamHandler : DoraemonEventService
    {
        public ConcurrentDictionary<Snowflake, int> UserMessages = new();
        public Timer Timer;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModerationConfiguration ModerationConfig { get; private set; } = new();

        public override int Priority => int.MaxValue - 2;

        private void SetTimer()
        {
            var timeSpan = TimeSpan.FromSeconds(ModerationConfig.SpamMessageTimeout);
            Timer = new Timer(_ => Task.Run(HandleTimerAsync), null, timeSpan, TimeSpan.FromSeconds(1));
        }

        private async Task HandleTimerAsync()
        {
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var usersToWarn = UserMessages.Where(x => x.Value > ModerationConfig.SpamMessageCountPerUser);
            var usersNotToWarn = UserMessages.Where(x => x.Value < ModerationConfig.SpamMessageCountPerUser);
            foreach (var user in usersToWarn.ToList())
            {
                var messageAuthor = guild.GetMember(user.Key);
                if (messageAuthor is null) continue;

                await InfractionService.CreateInfractionAsync(messageAuthor.Id, Bot.CurrentUser.Id, guild.Id,
                    InfractionType.Warn, "Spamming messages.", false, null);
                UserMessages.TryRemove(user.Key, out _);
                await Task.Delay(250);
            }

            foreach (var user in usersNotToWarn.ToList())
            {
                UserMessages.TryRemove(user.Key, out _);
            }
        }

        public DynamicSpamHandler(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {
            SetTimer();
        }

        protected override ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return ValueTask.CompletedTask;
            if (AuthorizationService.CurrentClaims.Contains(ClaimMapType.BypassAutoModeration)) return ValueTask.CompletedTask;
            if (eventArgs.Message is not IUserMessage message) return ValueTask.CompletedTask;
            if (message.Author.Id == Bot.CurrentUser.Id) return ValueTask.CompletedTask;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            if (message.Author.Id == guild.OwnerId) return ValueTask.CompletedTask;
            UserMessages.AddOrUpdate(message.Author.Id, 1, (_, oldValue) => oldValue + 1);
            return ValueTask.CompletedTask;
        }
    }
}