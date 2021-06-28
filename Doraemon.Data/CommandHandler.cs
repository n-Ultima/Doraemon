﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Doraemon.Common.Extensions;
using System.Text.RegularExpressions;
using Discord.Commands;
using System.Threading;
using Serilog;
using System.Reflection;
using Doraemon.Data.TypeReaders;
using Discord;
using Doraemon.Data.Models;
using Doraemon.Data.Events;
using Microsoft.Extensions.Configuration;
using Doraemon.Data.Services;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Events.MessageReceivedHandlers;
using Doraemon.Common;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Doraemon.Data
{
    public class CommandHandler : DiscordClientService
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        // Used for ping
        public static DateTime timeReceived;
        // Gets the provider for the bot.
        private readonly IServiceProvider _provider;
        // The bot account, or client.
        public static DiscordSocketClient _client;
        // Command service is for the bot to detect and execute commands.
        public static CommandService _service;
        // Used to read from the config file specified in Internals.cs
        public static IConfiguration _config;
        // Used for handling tag detection
        public static TagService tService;
        // Giga-chad move
        public IServiceScopeFactory _serviceScopeFactory;
        public GuildEvents _guildEvents;
        public InfractionService _infractionService;
        public UserEvents _userEvents;
        public CommandEvents _commandEvents;
        public AutoModeration _autoModeration;
        public TagHandler _tagHandler;
        public ModmailHandler _modmailHandler;
        public ILogger<CommandHandler> _logger; // I only DI this to satisfy DiscordClientService. We already use Serilog.
        // Inject everything like a champ
        public CommandHandler(ILogger<CommandHandler> logger, IServiceScopeFactory serviceScopeFactory, ModmailHandler modmailHandler, IServiceProvider provider, DiscordSocketClient client, CommandService service, IConfiguration config, TagService _tService, GuildEvents guildEvents, UserEvents userEvents, AutoModeration autoModeration, CommandEvents commandEvents, TagHandler tagHandler, InfractionService infractionService)
            : base(client, logger)
        {
            _modmailHandler = modmailHandler;
            _provider = provider;
            _client = client;
            _service = service;
            _config = config;
            tService = _tService;
            _guildEvents = guildEvents;
            _userEvents = userEvents;
            _commandEvents = commandEvents;
            _autoModeration = autoModeration;
            _tagHandler = tagHandler;
            _infractionService = infractionService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            // Dependency injection
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)// This overrides the InitializedServiece
        {
            _client.Ready += _guildEvents.ClientReady;
            // Fired when a message is received.
            _client.MessageReceived += _modmailHandler.ModmailAsync;

            _client.MessageReceived += _autoModeration.CheckForBlacklistedAttachmentTypesAsync;

            _client.MessageReceived += _autoModeration.CheckForRestrictedWordsAsync;

            _client.MessageReceived += _autoModeration.CheckForDiscordInviteLinksAsync;

            _client.MessageReceived += _autoModeration.CheckForSpamAsync;

            _client.MessageReceived += _tagHandler.CheckForTagsAsync;

            _client.MessageReceived += OnMessageReceived;

            _service.CommandExecuted += _commandEvents.OnCommandExecuted;
            // Fired when a user joins the guild.
            _client.UserJoined += _userEvents.UserJoined;
            // Fired when a message is edited

            _client.MessageUpdated += _guildEvents.MessageEdited;
            // Fired when a message is deleted
            _client.MessageDeleted += _guildEvents.MessageDeleted;
            
            // Start of new-mute handle method(darn you efehan)
            SetTimerAsync();

            _service.AddTypeReader<TimeSpan>(new TimeSpanTypeReader(), true);

            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            await StartRandomStatusAsync();

        }

        System.Timers.Timer Timer;
        /// <summary>
        /// Starts the timer for handling temporary infractions.
        /// </summary>
        private void SetTimerAsync() // New(switch to old one)
        {
            Timer = new System.Timers.Timer(30000);
            Timer.Enabled = true;
            Timer.AutoReset = true;
            Timer.Elapsed += CheckForExpiredInfractionsAsync;
        }
        private async Task StartRandomStatusAsync()
        {
            string[] statuses = new string[]
            {
                $"{DoraemonConfig.Prefix}help",
                "with modmail tickets",
                "with logs",
                "with That_One_Nerd's hair",
                "with trains",
                "with Ultima's dog"
            };
            var r = new Random();
            for(int i = 0; i < statuses.Length; i++)
            {
                var statusToSet = r.Next(0, statuses.Length);
                await _client.SetGameAsync(statuses[statusToSet]);
                await Task.Delay(30000);
            }
            await StartRandomStatusAsync();
        }
        /// <summary>
        /// Fired when a timer has elapsed.
        /// </summary>
        /// <param name="sender">The literal <see cref="object"/> that is needed for the function.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> that is fired whenever a timer has elapsed.</param>
        public async void CheckForExpiredInfractionsAsync(object sender, ElapsedEventArgs e)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var infractionService = scope.ServiceProvider.GetRequiredService<InfractionService>();
            var infractions = await infractionService.FetchTimedInfractionsAsync();
            if(infractions is not null)
            {
                foreach (var infraction in infractions)
                {
                    if (infraction.CreatedAt + infraction.Duration <= DateTime.Now)
                    {
                        await infractionService.RemoveInfractionAsync(infraction.Id, "Infraction Rescinded Automatically", _client.CurrentUser.Id, false);
                    }
                }
                // todo: remove stupid bool for save changes
                using var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.SaveChangesAsync();
            }

        }
        public async Task OnMessageReceived(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }
            string[] responses = new string[]
            {
                "I know we all have opinions, but this is blasphemy.",
                "A for effort.",
                "I gouged my eyes out reading this.",
                "Seems you just want a ban now don't you.",
                "I'm done.",
                "Pro Tip: Next time don't type with your eyes closed.",
                "Even though your entitled to your opinion, just know that no one agrees with that statement.",
                "I didn't know troglodites could spell."
            };
            if (!(arg is SocketUserMessage message)) return;
            timeReceived = DateTime.Now;
            if (message.Author.IsBot) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message);
            if (message.HasStringPrefix("ultima rate my", ref argPos, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                var r = new Random();
                var response = r.Next(0, responses.Length);
                await message.Channel.SendMessageAsync(responses[response]);
            }
            // Declare where the prefix should be looked for in the message.
            // If the message doesn't contain the prefix or a meniton of the bot, we return.
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await context.Channel.SendMessageAsync($"The prefix currently configured is `{DoraemonConfig.Prefix}`.");
            }
            if (!message.HasStringPrefix(_config["Prefix"], ref argPos)) return;
            // After all this, we execute the bot's startup.
            await _service.ExecuteAsync(context, argPos, _provider);
        }
    }
}
