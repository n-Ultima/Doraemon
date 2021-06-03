using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Attributes
{
    public class RequirePromotionAuthorization : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var StaffRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Staff");
            var AssociateRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Associate");
            if (context.User is SocketGuildUser gUser)
            {
                if (gUser.Roles.Contains(StaffRole) || gUser.Roles.Contains(AssociateRole))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("The following claims were missing: PromotionAuth"));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("Command was not run inside of a guild."));
            }
        }
    }
}
