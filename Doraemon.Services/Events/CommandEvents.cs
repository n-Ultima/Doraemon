using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Serilog;

namespace Doraemon.Services.Events
{
    [DoraemonService]
    public class CommandEvents
    {
        public async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result) // Fired when a command is executed.
        {
            // If it's a condition not met(like a user using mod commands, then delete the message
            if (result.Error == CommandError.UnmetPrecondition)
            {
                await context.Message.DeleteAsync();
                return;
            }

            // If the command does not exist, we just flag the message an an unknown command.
            if (!command.IsSpecified)
            {
                var emote = new Emoji("⚠️");
                await context.Message.AddReactionAsync(emote);
                return;
            }

            // If none of these are the case, we send an error message of what happened.
            if (command.IsSpecified && !result.IsSuccess)
            {
                Log.Logger.Error(
                    $"An error occured executing {command.Value.Name}\n\nCommand Error: {result.ErrorReason}");
                Log.Logger.Error($"\n\n{result.Error}");
                var emote = new Emoji("⚠️");
                await context.Message.AddReactionAsync(emote);
                if (result.Error == CommandError.Exception)
                {
                    await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
                    return;
                }
                await context.Channel.SendMessageAsync($"Error: {result}");
            }
        }
    }
}