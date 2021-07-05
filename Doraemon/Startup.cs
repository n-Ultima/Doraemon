using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;

using Doraemon.Common.CommandHelp;
using Doraemon.Data;
using Doraemon.Services.Events;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Common;
using Doraemon.Services;

using Interactivity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;
using System;
using Doraemon.Services.Moderation;
using Doraemon.Services.PromotionServices;
using Doraemon.Services.Core;
using Doraemon.Data.Repositories;

namespace Doraemon
{
    internal class Internals
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        static async Task Main()
        {
            var serilogConfig = Log.Logger = new LoggerConfiguration()
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
                .ConfigureDiscordHost((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 500,

                    };
                    config.Token = context.Configuration["token"];
                })
                .UseCommandService((context, config) =>
                {
                    config = new CommandServiceConfig()
                    {
                        CaseSensitiveCommands = false,
                        LogLevel = LogSeverity.Verbose,
                        DefaultRunMode = RunMode.Sync
                    };
                })
                .UseSerilog(serilogConfig)
                .ConfigureServices((context, services) =>
                {
                    services
                    .AddHostedService<CommandHandler>()
                    .AddHostedService<StatusService>()
                    .AddDbContext<DoraemonContext>(x =>
                        x.UseNpgsql(DoraemonConfig.DbConnection))
                    .AddSingleton<ICommandHelpService, CommandHelpService>()
                    .AddSingleton<InteractivityService>()
                    .AddSingleton(new InteractivityConfig
                    {
                        DefaultTimeout = TimeSpan.FromMinutes(2)
                    })
                    .AddScoped<InfractionService>()
                    .AddScoped<TagService>()
                    .AddScoped<AuthorizationService>()
                    .AddScoped<RoleClaimService>()
                    .AddScoped<TagHandler>()
                    .AddScoped<RoleClaimService>()
                    .AddScoped<GuildManagementService>()
                    .AddSingleton<HttpClient>()
                    .AddScoped<GuildEvents>()
                    .AddScoped<UserEvents>()
                    .AddSingleton<CommandEvents>()
                    .AddScoped<AutoModeration>()
                    .AddScoped<ModmailHandler>()
                    .AddScoped<ModmailTicketService>()
                    .AddScoped<GuildUserService>()
                    .AddScoped<PromotionService>()
                    // Repositoreies
                    .AddScoped<InfractionRepository>()
                    .AddScoped<ClaimMapRepository>()
                    .AddScoped<GuildRepository>()
                    .AddScoped<GuildUserRepository>()
                    .AddScoped<ModmailTicketRepository>()
                    .AddScoped<CampaignRepository>()
                    .AddScoped<CampaignCommentRepository>()
                    .AddScoped<TagRepository>();

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