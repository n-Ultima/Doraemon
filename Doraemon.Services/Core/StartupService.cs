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
            //await Bot.WaitUntilReadyAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var doraemonContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<DoraemonContext>())
                {
                    Log.Logger.Information($"Begin migrating database.");
                    try
                    {
                        await doraemonContext.Database.ExecuteSqlRawAsync("create extension citext;");
                    }
                    catch (NpgsqlException)
                    {
                        Log.Logger.Debug($"Attempted creating extension Citext, but it already exists.");
                    }

                    try
                    {
                        var migrationsToAdd = await doraemonContext.Database.GetPendingMigrationsAsync();
                        if (migrationsToAdd.Any())
                        {
                            await doraemonContext.Database.MigrateAsync();
                            Log.Logger.Information($"Migrations applied.");
                            return;
                        }

                        Log.Logger.Information("No migrations found.");
                    }
                    catch (NpgsqlException ex)
                    {
                        Log.Logger.Error(ex, "Failed migrating database.");
                    }

                    return;
                }
            }
        }
    }
}