using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Humanizer;
using Serilog;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class ClientReadyHandler : DoraemonEventService
    {
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        public ClientReadyHandler(AuthorizationService authorizationService, InfractionService infractionService)
            :base(authorizationService, infractionService)
        {}

        protected override async ValueTask OnReady(ReadyEventArgs e)
        {
            if (e.GuildIds.Count != 1)
            {
                throw new InvalidOperationException($"Doraemon should only be run in one guild.");
            }
            var guildToModifyId = e.GuildIds[0]; // only one guild per instance
            var guild = Bot.GetGuild(guildToModifyId);
            var channels = guild.GetChannels().Values.AsEnumerable();
            foreach (var channel in channels)
            {
                if (channel is not ITextChannel textChannel) return;
                var muteRole = guild.Roles.FirstOrDefault(x => x.Value.Name == muteRoleName).Value;
                await textChannel.SetOverwriteAsync(LocalOverwrite.Role(muteRole.Id, new OverwritePermissions(ChannelPermissions.None, Permission.SendMessages)));
                await textChannel.SetOverwriteAsync(LocalOverwrite.Role(guild.Id, new OverwritePermissions(ChannelPermissions.None, Permission.AddReactions)));

            }

            var humanizedChannels = channels.Select(x => x.Name);
            Log.Logger.Information($"Set {muteRoleName} role's overwrittes in channel(s): [{humanizedChannels.Humanize()}]");
        }
    }
}