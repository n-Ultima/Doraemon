using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.GatewayEventHandlers.BanGatewayEventHandlers
{
    public class BanCreatedHandler : DoraemonEventService
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public BanCreatedHandler(AuthorizationService authorizationService, InfractionService infractionService)
            : base(authorizationService, infractionService)
        {
        }

        /// <summary>
        /// Fired when a ban is created.
        /// </summary>
        /// <param name="eventArgs">The event data.</param>
        protected override async ValueTask OnBanCreated(BanCreatedEventArgs eventArgs)
        {
            var guild = Bot.GetGuild(eventArgs.GuildId);
            var auditLogs = await guild.FetchAuditLogsAsync<IMemberUnbannedAuditLog>(limit: 10);
            var ban = auditLogs
                .Where(x => x.TargetId == eventArgs.UserId)
                .Where(x => x.Actor.Id != Bot.CurrentUser.Id) // We don't want to handle bans done by the command, only by a user in the Discord UI.
                .FirstOrDefault();
            if (ban == null)
                return;
            var infractions = await InfractionService.FetchUserInfractionsAsync(ban.TargetId.Value);
            var banInfraction = infractions
                .Where(x => x.Type == InfractionType.Ban)
                .FirstOrDefault();
            // This means it occured inside of the Discord UI, which means, create the infraction.
            if (banInfraction == null)
            {
                await InfractionService.CreateInfractionAsync(ban.TargetId.Value, ban.ActorId.Value, ban.GuildId, InfractionType.Ban, ban.Reason ?? "No reason specified", false, null);
            }
        }
    }
}