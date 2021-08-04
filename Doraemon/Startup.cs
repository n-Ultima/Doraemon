﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Gateway.Default;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using Doraemon.Services;
using Doraemon.Services.GatewayEventHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Qmmands;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Filters;

namespace Doraemon
{
    internal class Internals
    {
        public static DoraemonConfiguration DoraemonConfig { get; } = new();
        public static ModerationConfiguration ModerationConfig { get; } = new();

        internal static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("config.json", false, false)
                        .Build();

                    x.AddConfiguration(configuration);
                })
                .ConfigureDiscordBot<DoraemonBot>((context, bot) =>
                {
                    bot.Token = DoraemonConfig.Token;
                    bot.Prefixes = new[]
                    {
                        DoraemonConfig.Prefix
                    };
                    
                    bot.Intents = GatewayIntents.All;
                    bot.ServiceAssemblies = new[]
                    {
                        typeof(DoraemonBot).Assembly,
                        typeof(AuthenticateUser).Assembly,
                        typeof(GuildUser).Assembly,
                        typeof(EmbedExtension).Assembly
                    }.ToList();
                })
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services
                        .Configure<DefaultGatewayCacheProviderConfiguration>(x => x.MessagesPerChannel = 200)
                        .AddSingleton<HttpClient>()
                        .AddDbContext<DoraemonContext>(x =>
                            x.UseNpgsql(DoraemonConfig.DbConnection))
                        .AddDbContextFactory<DoraemonContext>(x => x.UseNpgsql(DoraemonConfig.DbConnection))
                        .AddDoraemonServices()
                        .AddDoraemonRepositories();
                })
                
                .UseConsoleLifetime();
            var host = builder.Build();

            using (var doraemonContext = host.Services.CreateScope().ServiceProvider.GetRequiredService<DoraemonContext>())
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
            }
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
    
}