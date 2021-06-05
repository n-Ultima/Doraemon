using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;

namespace Doraemon.Common.Attributes
{
    public class RequireStaff : PreconditionAttribute
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(context.User is SocketGuildUser gUser)
            {
                var StaffRole = context.Guild.GetRole(DoraemonConfig.StaffRoleId);
                if (gUser.Roles.Contains(StaffRole))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("The following claims were missing: Staff"));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("The command was not run inside of a guild."));
            }
        }
    }
}
