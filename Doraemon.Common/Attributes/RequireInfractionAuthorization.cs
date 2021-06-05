using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;

namespace Doraemon.Common.Attributes
{
    public class RequireInfractionAuthorization : PreconditionAttribute
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var StaffRole = context.Guild.GetRole(DoraemonConfig.StaffRoleId);
            if(context.User is SocketGuildUser gUser)
            {
                if (gUser.Roles.Contains(StaffRole))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("The following claims were missing: InfractionAuthorization"));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("Command was not run inside of a guild."));
            }
        }
    }
}
