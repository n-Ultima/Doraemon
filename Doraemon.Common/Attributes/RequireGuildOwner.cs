using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Doraemon.Common.Attributes
{
    public class RequireGuildOwner : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(context.User is SocketGuildUser gUser)
            {
                if(context.Guild.OwnerId == gUser.Id)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("The following claims were missing: GuilldOwner"));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("Command was not run inside of a guild."));
            }
        }
    }
}
