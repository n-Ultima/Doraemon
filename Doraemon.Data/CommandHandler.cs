using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Doraemon.Data.Models.Core;
using System.Text.RegularExpressions;
using Discord.Commands;
using System.Threading;
using Serilog;
using System.Reflection;
using Discord;
using Doraemon.Data.Models;
using Doraemon.Data.Events;
using Microsoft.Extensions.Configuration;
using Doraemon.Data.Services;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Events.MessageReceivedHandlers;

namespace Doraemon.Data
{
    public class CommandHandler : InitializedService// InitializedService is used for the Microsoft IHostedService
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        // Used for ping
        public static DateTime timeReceived;
        // Gets the provider for the bot.
        private readonly IServiceProvider _provider;
        // The database
        public DoraemonContext _doraemonContext;
        // The bot account, or client.
        public static DiscordSocketClient _client;
        // Command service is for the bot to detect and execute commands.
        public static CommandService _service;
        // Used to read from the config file specified in Internals.cs
        public static IConfiguration _config;
        // Used for handling tag detection
        public static TagService tService;
        // Giga-chad move
        public GuildEvents _guildEvents;
        public UserEvents _userEvents;
        public CommandEvents _commandEvents;
        public AutoModeration _autoModeration;
        public TagHandler _tagHandler;
        // The list that handles mutes. Basically makes the mutes list.
        public static List<Mute> Mutes = new List<Mute>();
        // The list that handles temp-bans.
        public static List<Ban> Bans = new List<Ban>();
        // Inject everything like a champ.
        public CommandHandler(DoraemonContext doraemonContext, IServiceProvider provider, DiscordSocketClient client, CommandService service, IConfiguration config, TagService _tService, GuildEvents guildEvents, UserEvents userEvents, AutoModeration autoModeration, CommandEvents commandEvents, TagHandler tagHandler)
        {
            _doraemonContext = doraemonContext;
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
            // Dependency injection
        }
        public override async Task InitializeAsync(CancellationToken cancellationToken)// This overrides the InitializedServiece
        {
            await _client.SetGameAsync("!help");
            // Loads the warn file.
            _client.Ready += _guildEvents.ClientReady;
            // Fired when a message is received.
            _client.MessageReceived += OnMessageReceived;
            // Automoderation
            // Check for restricted words
            _client.MessageReceived += _autoModeration.CheckForRestrictedWordsAsync;
            // Check for Discord Invite Links
            _client.MessageReceived += _autoModeration.CheckForDiscordInviteLinksAsync;
            // Check for spam
            _client.MessageReceived += _autoModeration.CheckForSpamAsync;
            // Fired to check for tag's aswell.
            _client.MessageReceived += _tagHandler.CheckForTagsAsync;
            // Check for attachments
            _client.MessageReceived += _autoModeration.CheckForBlacklistedAttachmentTypesAsync;
            // Fired when a command is exectuted. We use service here because the Discord.NET command service is what detects when a command is attempted.
            _service.CommandExecuted += _commandEvents.OnCommandExecuted;
            // Fired when a user joins the guild.
            _client.UserJoined += _userEvents.UserJoined;
            // Fired when a message is edited
            // Fired when the client is disconnected
            _client.Connected += _client_Connected;
            _client.MessageUpdated += _guildEvents.MessageEdited;
            // Fired when a message is deleted
            _client.MessageDeleted += _guildEvents.MessageDeleted;
            // Starts the Mute Handler.
            Task.Run(async () => await MuteHandler());
            // Starts the Ban Handler.
            Task.Run(async () => await TempBanHandler());
            // Provides the modules necessary.
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }
        private Task _client_Connected()
        {
            Log.Logger.Information("The client has been successfully connected to the gateway.");
            return Task.CompletedTask;
        }
        private async Task TempBanHandler()
        {
            // This whole list follows the same concept as the mute handler.
            List<Ban> Remove = new List<Ban>();
            foreach (var ban in Bans)
            {
                // Checks if the user should be unbanned.
                if (DateTime.Now < ban.End)
                {
                    continue;
                }
                var guild = _client.GetGuild(ban.Guild.Id);
                if (guild.GetBanAsync(ban.User.Id) == null)
                {
                    Remove.Add(ban);
                    continue;
                }
                await guild.RemoveBanAsync(ban.User.Id, options: new RequestOptions()
                {
                    AuditLogReason = "Temporary Ban Timer Reached. Unbanning."
                });
                var inf = await _doraemonContext
                    .Set<Infraction>()
                    .AsQueryable()
                    .Where(x => x.Type == "Temporary Ban")
                    .Where(x => x.subjectId == ban.User.Id)
                    .FirstOrDefaultAsync();
                Remove.Add(ban);
                _doraemonContext.Infractions.Remove(inf);
                await _doraemonContext.SaveChangesAsync();
            }
            Bans = Bans.Except(Remove).ToList();
            await Task.Delay(1000);
            await TempBanHandler();
        }
        // The MuteHandler handles unmuting users after the time is up.
        private async Task MuteHandler()
        {
            List<Mute> Remove = new List<Mute>();
            foreach (var mute in Mutes)
            {
                if (DateTime.Now < mute.End)
                {
                    continue;
                }
                var guild = _client.GetGuild(mute.Guild.Id);
                if (guild.GetRole(mute.Role.Id) == null)
                {
                    Remove.Add(mute);
                    continue;
                }
                var role = guild.GetRole(mute.Role.Id);
                if (guild.GetUser(mute.User.Id) == null)
                {
                    Remove.Add(mute);
                    continue;
                }
                var user = guild.GetUser(mute.User.Id);
                if (role.Position > guild.CurrentUser.Hierarchy)
                {
                    Remove.Add(mute);
                    continue;
                }
                await user.RemoveRoleAsync(mute.Role);
                var inf = await _doraemonContext
                    .Set<Infraction>()
                    .AsQueryable()
                    .Where(x => x.Type == "Temporary Mute")
                    .Where(x => x.subjectId == mute.User.Id)
                    .FirstOrDefaultAsync();
                _doraemonContext.Remove(inf);
                await _doraemonContext.SaveChangesAsync();
                Remove.Add(mute);
            }
            Mutes = Mutes.Except(Remove).ToList();
            await Task.Delay(1000);
            await MuteHandler();
        }
        public async Task OnMessageReceived(SocketMessage arg)
        {
            timeReceived = DateTime.Now;
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_client, message);
            if (message.Channel.GetType() == typeof(SocketDMChannel))
            {
                await context.Channel.SendMessageAsync("Commands cannot be executed in DM's. To contact Staff, please message <@795766947794124820>.");
                return;
            }
            // Declare where the prefix should be looked for in the message.
            var argPos = 0;
            // If the message doesn't contain the prefix or a meniton of the bot, we return.
            if (!message.HasStringPrefix(_config["prefix"], ref argPos)) return;
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await context.Channel.SendMessageAsync($"My prefix is `{_config["prefix"]}`.");
            }
            // After all this, we execute the bot's startup.
            await _service.ExecuteAsync(context, argPos, _provider);
        }

    }
}
