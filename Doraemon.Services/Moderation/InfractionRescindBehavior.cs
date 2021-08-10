using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.Moderation
{
    /// <summary>
    ///  This behavior will automatically rescind infractions if the time has passed.
    /// </summary>
    public class InfractionRescindBehavior : DoraemonEventService
    {
        private readonly ClaimService _claimService;
        public InfractionRescindBehavior(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override int Priority => 24;

        //private void SetTimer()
        //{
        //    var timer = new Timer(_ => _ = Task.Run(BeginExecuteAsync), null, Interval, Timeout.InfiniteTimeSpan);
        //}
        public TimeSpan Interval = TimeSpan.FromSeconds(5);

        /// <summary>   
        /// Checks for expired infractions. If any are found, we set the timer to be called when the next one should be rescinded.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Bot.WaitUntilReadyAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var infractionService = scope.ServiceProvider.GetRequiredService<InfractionService>();
                    var timedInfractions = await infractionService.FetchTimedInfractionsAsync();
                    if (!timedInfractions.Any())
                    {
                        // We find ourselves in a situation where we have no timed infractions.
                        // In this case, we simply check again in 30 seconds.
                        // When there are infractions, we set the timer to trigger when the next infraction should expire.
                        await Task.Delay(Interval);
                        continue;
                    }
                    var expiringInfraction = timedInfractions
                        .OrderBy(x => x.ExpiresAt)
                        .FirstOrDefault();
                    // If it needs removed, then remove it, and set the timer to the next expiring infraction.
                    if (expiringInfraction.CreatedAt + expiringInfraction.Duration <= DateTimeOffset.UtcNow)
                    {
                        await infractionService.RemoveInfractionAsync(expiringInfraction.Id, "Infraction rescinded automatically.", Bot.CurrentUser.Id);
                        var timedInfractionsAfterRescind = await infractionService.FetchTimedInfractionsAsync();
                        var nextExpiringInfraction = timedInfractionsAfterRescind
                            .OrderBy(x => x.ExpiresAt)
                            .FirstOrDefault();
                        var time = nextExpiringInfraction.ExpiresAt - DateTimeOffset.UtcNow;
                        await Task.Delay(time.Value);
                        continue;

                    }
                    // It doesn't need removed, so simply set it to the rest of said-infractions' duration.
                    else
                    {
                        var timeToSet = expiringInfraction.ExpiresAt - DateTimeOffset.UtcNow;
                        await Task.Delay(timeToSet.Value);
                        continue;
                    }
                }
            }
        }
    }
}