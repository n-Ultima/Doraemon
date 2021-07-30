using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.CommandHelp;
using Doraemon.Data;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Doraemon.Services;
using Doraemon.Services.Core;
using Doraemon.Services.Events;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Services.Moderation;
using Doraemon.Services.PromotionServices;
using Interactivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                .Filter.ByExcluding(Matching.FromSource("Discord"))
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
                .ConfigureDiscordHost((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 500,
                    };
                    config.Token = DoraemonConfig.Token;
                })
                .UseCommandService((context, config) =>
                {
                    config.CaseSensitiveCommands = false;
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = RunMode.Sync;
                })
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services
                        .AddHostedService<CommandHandler>()
                        .AddHostedService<StatusService>()
                        .AddDbContext<DoraemonContext>(x =>
                            x.UseNpgsql(DoraemonConfig.DbConnection))
                        .AddSingleton<ICommandHelpService, CommandHelpService>()
                        .AddSingleton<InteractivityService>()
                        .AddSingleton<HttpClient>()
                        .AddSingleton(new InteractivityConfig
                        {
                            DefaultTimeout = TimeSpan.FromMinutes(2)
                        })
                        .AddDoraemonServices()
                        .AddDoraemonRepositories();
                })
                .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}