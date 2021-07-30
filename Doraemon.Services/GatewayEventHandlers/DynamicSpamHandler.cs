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
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Serilog;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class DynamicSpamHandler : DoraemonEventService
    {
        public ConcurrentDictionary<ulong, int> UserMessages = new();
        public Timer Timer;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModerationConfiguration ModerationConfig { get; private set; } = new();

        
        private void SetTimer()
        {
            var timeSpan = TimeSpan.FromSeconds(ModerationConfig.SpamMessageTimeout);
            Log.Logger.Information($"Started the anti-spam timer!\nDuration: {ModerationConfig.SpamMessageTimeout} seconds\n");
            Timer = new Timer(_ => _ = Task.Run(HandleTimerAsync), null, timeSpan, TimeSpan.FromSeconds(1));
        }

        private async Task HandleTimerAsync()
        {
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var usersToWarn = UserMessages.Where(x => x.Value >= ModerationConfig.SpamMessageCountPerUser);
            var usersNotToWarn = UserMessages.Where(x => x.Value < ModerationConfig.SpamMessageCountPerUser);
            foreach (var user in usersToWarn.ToList())
            {
                var messageAuthor = guild.GetMember(user.Key);
                if (messageAuthor is null) continue;
                
                await InfractionService.CreateInfractionAsync(messageAuthor.Id, Bot.CurrentUser.Id, guild.Id,
                    InfractionType.Warn, "Spamming messages.", false, null);
                UserMessages.Remove(user.Key, out var success);
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

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return;
            if (AuthorizationService.CurrentClaims.Contains(ClaimMapType.BypassAutoModeration)) return;
            if (eventArgs.Message is not IUserMessage message) return;
            UserMessages.AddOrUpdate(message.Author.Id, 1, (_, oldValue) => oldValue + 1);
        }
    }
}