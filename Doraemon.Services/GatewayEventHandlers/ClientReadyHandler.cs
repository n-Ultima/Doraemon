using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Data.Models.Moderation;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class ClientReadyHandler : DoraemonEventService
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly IHostApplicationLifetime _applicationLifetime;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ClientReadyHandler(AuthorizationService authorizationService, InfractionService infractionService, IHostApplicationLifetime applicationLifetime)
            : base(authorizationService, infractionService)
        {
            _applicationLifetime = applicationLifetime;
        }
        public override int Priority => 25;

        protected override async ValueTask OnReady(ReadyEventArgs e)
        {
            await Bot.SetPresenceAsync(UserStatus.Online, new LocalActivity("ultima.one/discord", ActivityType.Custom));
            if (e.GuildIds.Count != 1)
            {
                Log.Logger.Fatal($"This bot was designed for one-guild use, however it was found to be in {e.GuildIds.Count()}. Please remove this bot from all other guilds except one and restart.");
                _applicationLifetime.StopApplication();
            }

            var guildToModifyId = e.GuildIds[0]; // only one guild per instance
            if (guildToModifyId != DoraemonConfig.MainGuildId)
            {
                Log.Logger.Fatal($"The MainGuildId provided in config.json does not match the one guild the bot detected. Please ensure that the guildId provided is valid.");
                _applicationLifetime.StopApplication();
            }
            var guild = Bot.GetGuild(guildToModifyId);
            await Bot.Chunker.ChunkAsync(guild);
            Log.Logger.Information($"Successfully cached guild: {guild.Name}");
            var channels = guild.GetChannels().Values.AsEnumerable();
            List<string> modifiedChannels = new();
            foreach (var channel in channels)
            {
                if (channel is not ITextChannel textChannel) continue;
                var muteRole = guild.Roles.FirstOrDefault(x => x.Value.Name == muteRoleName).Value;
                await textChannel.SetOverwriteAsync(LocalOverwrite.Role(muteRole.Id, new OverwritePermissions(ChannelPermissions.None, Permission.SendMessages
                                                                                                                                       | Permission.AddReactions
                                                                                                                                       | Permission.UsePublicThreads
                                                                                                                                       | Permission.UsePrivateThreads)));
                modifiedChannels.Add(textChannel.Name);
            }

            var humanizedChannels = modifiedChannels.Humanize();
            Log.Logger.Information($"Successfully setup the mute-role for [{humanizedChannels}]");
            var botMember = guild.GetMember(Bot.CurrentUser.Id);
            await AuthorizationService.AssignCurrentUserAsync(Bot.CurrentUser.Id, botMember.RoleIds);

            var infractions = await InfractionService.FetchTimedInfractionsAsync();
            var infractionsToRescind = infractions
                .Where(x => x.CreatedAt + x.Duration <= DateTimeOffset.UtcNow)
                .ToList();
            if (!infractions.Any())
            {
                Log.Logger.Information($"No infractions found to be needing rescinded.");
                return;
            }
            foreach (var infractionToRescind in infractionsToRescind)
            {
                await InfractionService.RemoveInfractionAsync(infractionToRescind.Id, "Infraction rescinded automatically", Bot.CurrentUser.Id);
            }
            var humanizedInfractions = infractionsToRescind
                .Select(x => x.Id)
                .ToList();
            Log.Logger.Information($"Rescinded the following infractions because they expired while the bot was offline: [{humanizedInfractions.Humanize()}]");
        }
    }
}