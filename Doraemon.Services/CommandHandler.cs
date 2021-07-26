using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.TypeReaders;
using Doraemon.Services.Core;
using Doraemon.Services.Events;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Services.Moderation;
using Doraemon.Services.PromotionServices;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using Timer = System.Threading.Timer;

namespace Doraemon.Services
{
    public class CommandHandler : DiscordClientService
    {
        // Used for ping
        public static DateTime timeReceived;

        // The bot account, or client.
        public static DiscordSocketClient _client;

        // Command service is for the bot to detect and execute commands.
        public static CommandService _service;

        // Used to read from the config file specified in Internals.cs
        public static IConfiguration _config;

        // Used for handling tag detection
        public static TagService tService;

        public static Stopwatch stopwatch = new();

        // Gets the provider for the bot.
        private readonly IServiceProvider _provider;
        public AutoModeration _autoModeration;
        public CommandEvents _commandEvents;
        public GuildEvents _guildEvents;
        public GuildUserService _guildUserService;
        public InfractionService _infractionService;

        public ILogger<CommandHandler>
            _logger; // I only DI this to satisfy DiscordClientService. We already use Serilog.

        public ModmailHandler _modmailHandler;

        // Giga-chad move
        public IServiceScopeFactory _serviceScopeFactory;
        public TagHandler _tagHandler;
        public UserEvents _userEvents;
        private readonly AuthorizationService _authorizationService;


        // Inject everything like a champ
        public CommandHandler(ILogger<CommandHandler> logger, IServiceScopeFactory serviceScopeFactory,
            ModmailHandler modmailHandler, IServiceProvider provider, DiscordSocketClient client,
            CommandService service, IConfiguration config, TagService _tService, GuildEvents guildEvents,
            UserEvents userEvents, AutoModeration autoModeration, CommandEvents commandEvents, TagHandler tagHandler,
            InfractionService infractionService, GuildUserService guildUserService, AuthorizationService authorizationService)
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
            _guildUserService = guildUserService;
            _authorizationService = authorizationService;
            // Dependency injection
        }

        public static DoraemonConfiguration DoraemonConfig { get; } = new();

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) // This overrides the DiscordClientService
        {
            _client.Ready += _guildEvents.ClientReady;
            _client.Disconnected += ClientOnDisconnected;
            // Fired when a message is received.

            _client.MessageReceived += HandleAuthenticationAsync;

            _client.MessageReceived += _modmailHandler.ModmailAsync;

            _client.MessageReceived += _autoModeration.CheckForMultipleMessageSpamAsync;

            _client.MessageReceived += _autoModeration.CheckForBlacklistedAttachmentTypesAsync;

            _client.MessageReceived += _autoModeration.CheckForRestrictedWordsAsync;

            _client.MessageReceived += _autoModeration.CheckForDiscordInviteLinksAsync;

            _client.MessageReceived += _autoModeration.CheckForSpamAsync;

            _client.MessageReceived += _tagHandler.CheckForTagsAsync;

            _client.MessageReceived += UpdateGuildUserAsync;

            _client.MessageReceived += OnMessageReceived;

            _client.MessageReceived += FinishHandleMessage;
            _service.CommandExecuted += _commandEvents.OnCommandExecuted;
            // Fired when a user joins the guild.
            _client.UserJoined += _userEvents.UserJoined;
            // Fired when a message is edited

            _client.MessageUpdated += _guildEvents.MessageEdited;

            _client.MessageUpdated += _modmailHandler.HandleEditedModmailMessageAsync;
            // Fired when a message is deleted
            _client.MessageDeleted += _guildEvents.MessageDeleted;

            // Start of new-mute handle method(darn you efehan)
            SetTimer();

            await AutoMigrateDatabaseAsync();
            stopwatch.Start();

            _service.AddTypeReader<TimeSpan>(new TimeSpanTypeReader(), true);
            _service.AddTypeReader<UserOrMessageAuthor>(new UserOrMessageAuthorEntityTypeReader());
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private Task ClientOnDisconnected(Exception ex)
        {
            if (ex is GatewayReconnectException)
            {
                Log.Logger.Information($"Received a reconnect request from Discord.");
                return Task.CompletedTask;
            }

            Log.Logger.Information(ex, $"The bot disconnected unexpectedly. Stopping the application.");
            return Task.CompletedTask;
        }

        private async Task HandleAuthenticationAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_client, message);
            if (context.User is not SocketGuildUser user) return;
            var roles = user.Roles.Select(x => x.Id);
            await _authorizationService.AssignCurrentUserAsync(message.Author.Id, roles);
            Log.Logger.Debug($"Received a MessageReceived event.");
        }

        private async Task AutoMigrateDatabaseAsync()
        {
            var scope = _serviceScopeFactory.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
            try
            {
                var migrations = await database.Database.GetPendingMigrationsAsync();
                // We attempt to create the citext extension.
                try
                {
                    database.Database.ExecuteSqlRaw($"CREATE EXTENSION citext;");
                    Log.Logger.Information($"Extension CITEXT created!");
                }
                catch (NpgsqlException)
                {
                    Log.Logger.Information($"Extension citext already exists!");
                }

                if (!migrations.Any())
                {
                    return;
                }

                Log.Logger.Information($"Migrations Found: {migrations.Humanize()}");
                await database.Database.MigrateAsync();
                Log.Logger.Information($"Migrations applied.");
            }
            catch(NpgsqlException ex)
            {
                Log.Logger.Error(ex, $"Error migrating the database!");
            }
        }

        

        private Timer Timer;
        private void SetTimer()
        {
            var autoEvent = new AutoResetEvent(false);
            var timeSpan = TimeSpan.FromSeconds(30);
            Timer = new Timer(_ => _ = Task.Run(CheckForExpiredInfractionsAsync), autoEvent, timeSpan, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        ///     Fired when a timer has elapsed.
        /// </summary>
        public async Task CheckForExpiredInfractionsAsync()
        {
            var infractions = await _infractionService.FetchTimedInfractionsAsync();
            var infractionsToRescind = infractions
                .Where(x => x.CreatedAt + x.Duration <= DateTimeOffset.UtcNow)
                .AsEnumerable();
            if (infractionsToRescind.Any())
            {
                foreach (var infraction in infractionsToRescind)
                {
                    switch (infraction.Type)
                    {
                        case InfractionType.Ban:
                            await _infractionService.RemoveInfractionAsync(infraction.Id, "Ban timed out.", _client.CurrentUser.Id);
                            break;
                        case InfractionType.Mute:
                            await _infractionService.RemoveInfractionAsync(infraction.Id, "Mute expired.", _client.CurrentUser.Id);
                            break;
                    }

                    Log.Logger.Information($"User: {_client.GetGuild(DoraemonConfig.MainGuildId).GetUser(infraction.SubjectId).GetFullUsername()} had their {infraction.Type} infraction rescinded automatically.");
                }
            }
        }

        public async Task UpdateGuildUserAsync(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message)) return;
            if (message.Channel.GetType() == typeof(SocketDMChannel)) return;

            var userToUpdate = await _guildUserService.FetchGuildUserAsync(arg.Author.Id);
            if (userToUpdate is null)
            {
                await _guildUserService.CreateGuildUserAsync(arg.Author.Id, arg.Author.Username,
                    arg.Author.Discriminator, false);
            }
            else
            {
                if (userToUpdate.Username != arg.Author.Username)
                    await _guildUserService.UpdateGuildUserAsync(arg.Author.Id, arg.Author.Username, null, null);
                if (userToUpdate.Discriminator != arg.Author.Discriminator)
                    await _guildUserService.UpdateGuildUserAsync(arg.Author.Id, null, arg.Author.Discriminator, null);
            }
        }

        public async Task OnMessageReceived(SocketMessage arg)
        {
            if (arg.Channel is SocketDMChannel) return;
            if (!(arg is SocketUserMessage message)) return;
            timeReceived = DateTime.Now;
            if (message.Author.IsBot) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message);
            // Declare where the prefix should be looked for in the message.
            // If the message doesn't contain the prefix or a meniton of the bot, we return.
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                await context.Channel.SendMessageAsync(
                    $"The prefix currently configured is `{DoraemonConfig.Prefix}`.");
            if (!message.HasStringPrefix(_config["Prefix"], ref argPos)) return;
            // After all this, we execute the bot's startup.
            await _service.ExecuteAsync(context, argPos, _provider);
        }

        public Task FinishHandleMessage(SocketMessage arg)
        {
            Log.Logger.Debug($"Finished handling the MessageReceived event.");
            return Task.CompletedTask;
        }
    }
}