﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Models;
using Disqord.Rest;
using Disqord.Rest.Api;
using Doraemon.Data.Models.Moderation;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class ClientReadyHandler : DoraemonEventService
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";

        public ClientReadyHandler(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {
        }

        public override int Priority => 25;

        protected override async ValueTask OnReady(ReadyEventArgs e)
        {
            await Bot.SetPresenceAsync(UserStatus.Online, new LocalActivity("with the hammer", ActivityType.Playing));
            if (e.GuildIds.Count != 1)
            {
                throw new InvalidOperationException($"Doraemon should only be run in one guild.");
            }

            var guildToModifyId = e.GuildIds[0]; // only one guild per instance
            var guild = Bot.GetGuild(guildToModifyId);
            await Bot.Chunker.ChunkAsync(guild);
            Log.Logger.Information($"Successfully cached guild: {guild.Name}");
            var channels = guild.GetChannels().Values.AsEnumerable();
            List<string> modifiedChannels = new();
            foreach (var channel in channels)
            {
                if (channel is not ITextChannel textChannel) continue;
                var muteRole = guild.Roles.FirstOrDefault(x => x.Value.Name == muteRoleName).Value;
                await textChannel.SetOverwriteAsync(LocalOverwrite.Role(muteRole.Id, new OverwritePermissions(ChannelPermissions.None, Permission.SendMessages)));
                await textChannel.SetOverwriteAsync(LocalOverwrite.Role(muteRole.Id, new OverwritePermissions(ChannelPermissions.None, Permission.AddReactions)));
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
            }

            else
            {
                foreach (var infractionToRescind in infractionsToRescind)
                {
                    await InfractionService.RemoveInfractionAsync(infractionToRescind.Id, "Infraction rescinded automatically", Bot.CurrentUser.Id);
                }
            }

            var humanizedInfractions = infractionsToRescind
                .Select(x => x.Id)
                .ToList();
            Log.Logger.Information($"Rescinded the following infractions because they expired while the bot was offline: [{humanizedInfractions.Humanize()}]");
            
            // Begin slash
            var apiClient = (Bot as IRestClient).ApiClient;
            var registeredGuildCommands = await apiClient.FetchGuildApplicationCommandsAsync(Bot.CurrentUser.Id, guild.Id);
            if (registeredGuildCommands.Any())
                return;
            // Begin creating slash commands
            var content = new CreateApplicationCommandJsonRestRequestContent
            {
                Name = "attachment-blacklists",
                Description = "Shows all forbidden attachment blacklists in the guild.",
            };
            await apiClient.CreateGuildApplicationCommandAsync(Bot.CurrentUser.Id, guild.Id, content);
        }
    }
}