using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class OnBanDeletedHandler : DoraemonEventService
    {
        public OnBanDeletedHandler(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {
        }

        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        protected override async ValueTask OnBanDeleted(BanDeletedEventArgs eventArgs)
        {
            var guild = Bot.GetGuild(eventArgs.GuildId);
            var auditLogs = await guild.FetchAuditLogsAsync<IMemberUnbannedAuditLog>();
            var mostRecentLog = auditLogs
                .Where(x => x.TargetId == eventArgs.UserId)
                .FirstOrDefault();
            if (mostRecentLog == null)
            {
                // WHy do we have an unban, but no audit log?
                // It could be a very old ban. But, we should try to find the infraction anyway.
                var unknownUserUnbanInfractions = await InfractionService.FetchUserInfractionsAsync(eventArgs.UserId);
                var unknownBanInfraction = unknownUserUnbanInfractions
                    .Where(x => x.Type == InfractionType.Ban)
                    .FirstOrDefault();
                // This means that it was executed in the UI, as the "!unban <userId> <reason>" would rescind the infraction.
                if (unknownBanInfraction != null)
                {
                    // Really dumb, but we need to reload our entities to make sure. If the `unban <userId> <reason> command is ran, the entities are never "reloaded" properly.
                    // We need to revoke the ban.
                    var reloadEntities = await InfractionService.FetchUserInfractionsAsync(eventArgs.UserId);
                    unknownBanInfraction = reloadEntities
                        .Where(x => x.Type == InfractionType.Ban)
                        .FirstOrDefault();
                    if (unknownBanInfraction != null)
                    {
                        await InfractionService.RemoveInfractionAsync(unknownBanInfraction.Id, "No reason specified.", unknownBanInfraction.ModeratorId);
                    }
                    else
                    {
                        // Don't worry about rescinding. Just return after logging.
                        var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                        await modLog.SendRescindedInfractionLogMessageAsync("Unknown reason and moderator. Please check audit logs.", Bot.CurrentUser.Id, eventArgs.UserId, "Ban", Bot);
                        return;
                    }
                }
                else
                {
                    // Don't worry about rescinding. Just return after logging.
                    var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                    await modLog.SendRescindedInfractionLogMessageAsync("Unknown reason and moderator. Please check audit logs.", Bot.CurrentUser.Id, eventArgs.UserId, "Ban", Bot);
                    return;
                }
            }

            // The audit log isn't null.
            var userInfractions = await InfractionService.FetchUserInfractionsAsync(mostRecentLog.TargetId.Value);
            var banInfraction = userInfractions
                .Where(x => x.Type == InfractionType.Ban)
                .FirstOrDefault();
            if (banInfraction == null)
            {
                // This means that one of two things happened:
                // 1. The infraction was never created, either from the bot being offline when the ban occured.
                // 2. The unban has occured through the Discord UI, which we want. This means we don't need to do anything.
                var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await modLog.SendRescindedInfractionLogMessageAsync("Unknown reason and moderator. Please check audit logs.", Bot.CurrentUser.Id, eventArgs.UserId, "Ban", Bot);
                return;
            }
            else
            {
                await InfractionService.RemoveInfractionAsync(banInfraction.Id, mostRecentLog.Reason, mostRecentLog.ActorId.Value);
            }
        }
    }
}