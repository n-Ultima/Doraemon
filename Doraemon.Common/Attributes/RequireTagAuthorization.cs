using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Attributes
{
    public class RequireTagAuthorization : PreconditionAttribute
    {
        public DoraemonConfiguration DoraemonConfg { get; private set; } = new();
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(context.User is SocketGuildUser gUser)
            {
                var StaffRole = context.Guild.GetRole(DoraemonConfg.StaffRoleId);
                var Associate = context.Guild.GetRole(DoraemonConfg.PromotionRoleId);

                if (gUser.Roles.Contains(StaffRole) || gUser.Roles.Contains(Associate))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("The following claims were missing: TagManage"));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("Command was not run inside of a guild."));
            }
        }
    }
}
