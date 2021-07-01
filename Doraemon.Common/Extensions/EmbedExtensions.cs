using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;

namespace Doraemon.Common.Extensions
{
    public static class EmbedExtension
    {
        public static async Task<IMessage> SendModSuccessAsync(this ISocketMessageChannel channel, string title, string description)//Sends the Success Message, but with the Mod Log footer.
        {
            var e = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(title)
                .WithDescription(description)
                .WithFooter(x =>
                {
                    x
                    .WithText("Mod Log");
                });
            var message = await channel.SendMessageAsync(embed: e.Build());
            return message;
        }
        public static async Task<IMessage> SendErrorMessageAsync(this ISocketMessageChannel channel, string title, string description)
        {
            var e = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(title)
                .WithDescription(description);
            var message = await channel.SendMessageAsync(embed: e.Build());
            return message;
        }

        public static async Task<IMessage> SendRescindedInfractionLogMessageAsync(this ISocketMessageChannel channel, string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordSocketClient client, string infractionId = null)
        {
            string format;
            var subjectUser = await client.Rest.GetUserAsync(subjectId);
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);

            switch (infractionType)
            {
                case "Ban":
                    format = "unbanned";
                    break;
                case "Mute":
                    format = "unmuted";
                    break;
                case "Warn":
                    format = "warned";
                    break;
                default:
                    format = "undefined";
                    break;
            }
            var builder = new StringBuilder();
            if(infractionType != "Warn")
            {
                builder.Append($"`{DateTimeOffset.Now}`{GetEmojiForRescindedInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
            else
            {
                builder.Append($"`{DateTimeOffset.Now}` {GetEmojiForRescindedInfractionType("Warn")} Punishment ID `{infractionId}` was removed by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
        }

        public static async Task<IMessage> SendInfractionLogMessageAsync(this ISocketMessageChannel channel, string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordSocketClient client, string duration = null)
        {
            if (infractionType == "Note") return null;
            string format;
            var subjectUser = await client.Rest.GetUserAsync(subjectId);
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);
            switch (infractionType)
            {
                case "Ban":
                    format = "banned";
                    break;
                case "Mute":
                    format = "muted";
                    break;
                case "Warn":
                    format = "warned";
                    break;
                default:
                    format = "undefined";
                    break;
            }
            var builder = new StringBuilder();
            if(duration is null)
            {
                builder.Append($"`{DateTimeOffset.Now}`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
            else
            {
                builder.Append($"`{DateTimeOffset.Now}`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**`({subjectUser.Id}`) was {format} for **{duration}** by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
        }
        private static string GetEmojiForInfractionType(string infractionType)
            => infractionType switch
            {
                "Note" => "📝",
                "Warn" => "⚠️",
                "Mute" => "🔇",
                "Ban" => "🔨",
                "Kick" => "👢",
                _ => "❔",
            };
        private static string GetEmojiForRescindedInfractionType(string infractionType)
            => infractionType switch
            {
                "Warn" => "❗",
                "Mute" => "🔊",
                "Ban" => "🔓",
                _ => "❓",
            };
    }
}

