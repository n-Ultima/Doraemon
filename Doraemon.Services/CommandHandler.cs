using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Data;
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
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using Timer = System.Timers.Timer;

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


        private Timer Timer;

        // Inject everything like a champ
        public CommandHandler(ILogger<CommandHandler> logger, IServiceScopeFactory serviceScopeFactory,
            ModmailHandler modmailHandler, IServiceProvider provider, DiscordSocketClient client,
            CommandService service, IConfiguration config, TagService _tService, GuildEvents guildEvents,
            UserEvents userEvents, AutoModeration autoModeration, CommandEvents commandEvents, TagHandler tagHandler,
            InfractionService infractionService, GuildUserService guildUserService)
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
            // Dependency injection
        }

        public static DoraemonConfiguration DoraemonConfig { get; } = new();

        protected override async Task
            ExecuteAsync(CancellationToken cancellationToken) // This overrides the InitializedServiece
        {
            _client.Ready += _guildEvents.ClientReady;
            // Fired when a message is received.
            _client.MessageReceived += _modmailHandler.ModmailAsync;

            _client.MessageReceived += _autoModeration.CheckForMultipleMessageSpamAsync;
            
            _client.MessageReceived += _autoModeration.CheckForBlacklistedAttachmentTypesAsync;

            _client.MessageReceived += _autoModeration.CheckForRestrictedWordsAsync;

            _client.MessageReceived += _autoModeration.CheckForDiscordInviteLinksAsync;

            _client.MessageReceived += _autoModeration.CheckForSpamAsync;

            _client.MessageReceived += _tagHandler.CheckForTagsAsync;

            _client.MessageReceived += UpdateGuildUserAsync;

            _client.MessageReceived += OnMessageReceived;

            _service.CommandExecuted += _commandEvents.OnCommandExecuted;
            // Fired when a user joins the guild.
            _client.UserJoined += _userEvents.UserJoined;
            // Fired when a message is edited

            _client.MessageUpdated += _guildEvents.MessageEdited;

            _client.MessageUpdated += _modmailHandler.HandleEditedModmailMessageAsync;
            // Fired when a message is deleted
            _client.MessageDeleted += _guildEvents.MessageDeleted;

            // Start of new-mute handle method(darn you efehan)
            SetTimerAsync();

            stopwatch.Start();
            
            await AutoMigrateDatabaseAsync();
            _service.AddTypeReader<TimeSpan>(new TimeSpanTypeReader(), true);

            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

        }

        private async Task AutoMigrateDatabaseAsync()
        {
            var scope = _serviceScopeFactory.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
            try 
            {
                await database.Database.EnsureCreatedAsync();
                var migrations = await database.Database.GetPendingMigrationsAsync();
                // We attempt to create the citext extension.
                try
                {
                    database.Database.ExecuteSqlRaw($"CREATE EXTENSION citext;");
                    Log.Logger.Information($"Extension CITEXT created!");
                }
                catch (NpgsqlException ex)
                {
                    Log.Logger.Information($"Extension CITEXT already exists!");
                }
                if (!migrations.Any())
                {
                    return;
                }
                Log.Logger.Information($"Migrations Found: {migrations.Humanize()}");
                await database.Database.MigrateAsync();
                Log.Logger.Information($"Migrations applied.");
            }
            catch
            {
                Log.Logger.Error($"Error migrating the database!");
                
            }
        }

        /// <summary>
        ///     Starts the timer for handling temporary infractions.
        /// </summary>
        private void SetTimerAsync() // New(switch to old one)
        {
            Timer = new Timer(30000);
            Timer.Enabled = true;
            Timer.AutoReset = true;
            Timer.Elapsed += CheckForExpiredInfractionsAsync;
        }

        /// <summary>
        ///     Fired when a timer has elapsed.
        /// </summary>
        /// <param name="sender">The literal <see cref="object" /> that is needed for the function.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> that is fired whenever a timer has elapsed.</param>
        public async void CheckForExpiredInfractionsAsync(object sender, ElapsedEventArgs e)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var infractionService = scope.ServiceProvider.GetRequiredService<InfractionService>();
            var infractions = await infractionService.FetchTimedInfractionsAsync();
            if (infractions is not null)
            {
                foreach (var infraction in infractions)
                    if (infraction.CreatedAt + infraction.Duration <= DateTime.Now)
                        await infractionService.RemoveInfractionAsync(infraction.Id,
                            "Infraction Rescinded Automatically", _client.CurrentUser.Id);
                // todo: remove stupid bool for save changes
                await using var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.SaveChangesAsync();
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
            if (arg.Channel.GetType() == typeof(SocketDMChannel)) return;
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
    }
}