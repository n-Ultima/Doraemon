using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Attributes
{
    public class RequireInfractionAuthorization : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(context.User is SocketGuildUser gUser)
            {
                if (gUser.GuildPermissions.ViewAuditLog)
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
