using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers;
using Serilog;

namespace Doraemon.Services.Moderation
{
    /// <summary>
    ///  This behavior will automatically rescind infractions if the time has passed.
    /// </summary>
    public class InfractionRescindBehavior : DoraemonEventService
    {
        public InfractionRescindBehavior(AuthorizationService authService, InfractionService infractService)
            : base(authService, infractService)
        {
            SetTimer();
        }

        public TimeSpan Interval = TimeSpan.FromSeconds(30);
        public Timer Timer;

        private void SetTimer()
        {
            var autoEvent = new AutoResetEvent(false);
            Timer = new Timer(_ => _ = Task.Run(CheckForExpiredInfractionsAsync), autoEvent, Interval, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Checks for expired infractions. If any are found, we set the timer to be called when the next one should be rescinded.
        /// </summary>
        private async Task CheckForExpiredInfractionsAsync()
        {
            var timedInfractions = await InfractionService.FetchTimedInfractionsAsync();
            if (!timedInfractions.Any())
                return;
            var expiringInfraction = timedInfractions
                .OrderBy(x => x.ExpiresAt)
                .FirstOrDefault();
            if (expiringInfraction.CreatedAt + expiringInfraction.Duration <= DateTimeOffset.UtcNow)
            {
                await InfractionService.RemoveInfractionAsync(expiringInfraction.Id, "Infraction rescinded automatically.", Bot.CurrentUser.Id);
            }

            var time = expiringInfraction.ExpiresAt - DateTimeOffset.UtcNow;
            Timer.Change(time.Value, Timeout.InfiniteTimeSpan);
        }
    }
}