using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Doraemon.Common;
using Microsoft.Extensions.Logging;

namespace Doraemon.Data
{
    public class StatusService : DiscordClientService
    {
        private readonly DoraemonConfiguration DoraemonConfig {get; private set;} = new();

        private readonly IReadOnlyList<string> _statuses;

        private readonly Random _random = new Random();

        public StatusService(DiscordSocketClient client, ILogger<DiscordClientService> logger) : base(client, logger)
        {
            _statuses = new List<string>()
            {
                $"{DoraemonConfig.Prefix}help",
                "with modmail tickets",
                "with logs",
                "with That_One_Nerd's hair",
                "with trains",
                "with Ultima's dog"
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Client.SetGameAsync(_statuses[_random.Next(_statuses.Count)]);

                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}
