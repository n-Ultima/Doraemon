using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class UserJoinedHandler : DoraemonEventService
    {
        private readonly GuildUserService _guildUserService;
        private const string muteRoleName = "Doraemon_Moderation_Mute";
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public UserJoinedHandler(AuthorizationService authorizationService, InfractionService infractionService, GuildUserService guildUserService)
            : base(authorizationService, infractionService)
        {
            _guildUserService = guildUserService;
        }

        protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
        {
            var trackedUser = await _guildUserService.FetchGuildUserAsync(e.Member.Id);
            if (trackedUser == null)
            {
                await _guildUserService.CreateGuildUserAsync(e.Member.Id, e.Member.Name, e.Member.Discriminator, false);
            }
            trackedUser = await _guildUserService.FetchGuildUserAsync(e.Member.Id); // re-query for actual results now
            var guild = Bot.GetGuild(e.GuildId);
            var trackedUserInfractions = await InfractionService.FetchUserInfractionsAsync(trackedUser.Id);
            var trackedTimeInfraction = trackedUserInfractions
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.CreatedAt + x.Duration >= DateTimeOffset.Now)
                .FirstOrDefault();
            if (trackedTimeInfraction != null)
            {
                var muteRole = guild.Roles.FirstOrDefault(x => x.Value.Name == muteRoleName).Value;
                await Bot.GrantRoleAsync(guild.Id, trackedUser.Id, muteRole.Id);
                var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await modLog.SendInfractionLogMessageAsync("Reapplied active mute.", Bot.CurrentUser.Id, trackedUser.Id, "Mute", Bot, null);
            }

            var userJoinedLog = new LocalEmbed()
                .WithTitle("User Joined")
                .AddField("Username", e.Member.Name)
                .AddField("Discriminator", e.Member.Discriminator)
                .AddField("Creation", e.Member.CreatedAt())
                .AddField("ID", e.Member.Id)
                .WithColor(DColor.Green);
            await Bot.SendMessageAsync(DoraemonConfig.LogConfiguration.UserJoinedLogChannelId, new LocalMessage()
                .WithEmbeds(userJoinedLog));
        }
    }
}