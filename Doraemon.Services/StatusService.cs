using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Doraemon.Common;
using Microsoft.Extensions.Logging;

namespace Doraemon.Services
{
    public class StatusService : DiscordClientService
    {
        private readonly Random _random = new();

        private readonly IReadOnlyList<string> _statuses;

        public StatusService(DiscordSocketClient client, ILogger<DiscordClientService> logger) : base(client, logger)
        {
            _statuses = new List<string>
            {
                $"{DoraemonConfig.Prefix}help",
                "with modmail tickets",
                "with logs",
                "with That_One_Nerd's hair",
                "with trains",
                "with Ultima's dog",
            };
        }

        private DoraemonConfiguration DoraemonConfig { get; } = new();

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