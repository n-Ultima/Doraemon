using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Doraemon.Common.Attributes
{
    /// <summary>
    ///     Requires the command to be ran by the owner of the guild.
    /// </summary>
    public class RequireGuildOwner : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (context.Guild.OwnerId == gUser.Id)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                return Task.FromResult(PreconditionResult.FromError("The following claims were missing: GuilldOwner"));
            }

            return Task.FromResult(PreconditionResult.FromError("Command was not run inside of a guild."));
        }
    }
}