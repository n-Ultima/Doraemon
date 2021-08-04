using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Doraemon.Data;
using Doraemon.Services.GatewayEventHandlers;
using Doraemon.Services.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

namespace Doraemon.Services.Core
{
    public class StartupService : DoraemonEventService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(AuthorizationService authorizationService, InfractionService infractionService, IServiceProvider serviceProvider)
            : base(authorizationService, infractionService)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //var infractions = await InfractionService.FetchTimedInfractionsAsync();
            //var infractionsToRescind = infractions
            //    .Where(x => x.CreatedAt + x.Duration <= DateTimeOffset.UtcNow)
            //    .ToList();
            //foreach(var infractionToRescind in infractionsToRescind)
            //{
            //    await InfractionService.RemoveInfractionAsync(infractionToRescind.Id, "Infraction rescinded automatically", infractionToRescind.ModeratorId);
            //}
        }
    }
}

