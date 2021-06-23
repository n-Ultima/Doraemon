using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Services;
using Doraemon.Data.Models;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Doraemon.Common.Utilities;
using Doraemon.Data.Events.MessageReceivedHandlers;
using Discord.Net;
using System.Text.RegularExpressions;
using Serilog;
using Doraemon.Common.Extensions;

namespace Doraemon.Data.Events
{
    public class CommandEvents
    {
        public async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)// Fired when a command is executed.
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
                Log.Logger.Error($"An error occured executing {command.Value.Name}\n\nCommand Error: {result.ErrorReason}");
                Log.Logger.Error($"\n\n{result.Error}");
                var emote = new Emoji("⚠️");
                await context.Message.AddReactionAsync(emote);
                await context.Channel.SendMessageAsync($"Error: {result}");
            }
        }
    }
}
