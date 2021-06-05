﻿using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;

using Doraemon.Common.CommandHelp;
using Doraemon.Data;
using Doraemon.Data.Events;
using Doraemon.Data.Events.MessageReceivedHandlers;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Doraemon
{
    class Internals
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        static async Task Main()
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
                .ConfigureDiscordHost<DiscordSocketClient>((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 500,
                        GatewayIntents =
                            GatewayIntents.GuildBans |              // GUILD_BAN_ADD, GUILD_BAN_REMOVE
                            GatewayIntents.GuildMembers |           // GUILD_MEMBER_ADD, GUILD_MEMBER_UPDATE, GUILD_MEMBER_REMOVE
                            GatewayIntents.GuildMessageReactions |  // MESSAGE_REACTION_ADD, MESSAGE_REACTION_REMOVE,
                                                                    //     MESSAGE_REACTION_REMOVE_ALL, MESSAGE_REACTION_REMOVE_EMOJI
                            GatewayIntents.GuildMessages |          // MESSAGE_CREATE, MESSAGE_UPDATE, MESSAGE_DELETE, MESSAGE_DELETE_BULK
                            GatewayIntents.Guilds |
                            GatewayIntents.DirectMessages           //     GUILD_ROLE_UPDATE, GUILD_ROLE_DELETE, CHANNEL_CREATE, CHANNEL_UPDATE,
                                                                    //     CHANNEL_DELETE, CHANNEL_PINS_UPDATE
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
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services
                    .AddHostedService<CommandHandler>()
                    .AddDbContext<DoraemonContext>(x =>
                        x.UseNpgsql(DoraemonConfig.DbConnection))
                    .AddSingleton<ICommandHelpService, CommandHelpService>()
                    .AddSingleton<TagService>()
                    .AddSingleton<InfractionService>()
                    .AddSingleton<TagHandler>()
                    .AddSingleton<GuildService>()
                    .AddSingleton<HttpClient>()
                    .AddSingleton<GuildEvents>()
                    .AddSingleton<UserEvents>()
                    .AddSingleton<CommandEvents>()
                    .AddSingleton<AutoModeration>()
                    .AddSingleton<ModmailHandler>()
                    .AddSingleton<PromotionService>();
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