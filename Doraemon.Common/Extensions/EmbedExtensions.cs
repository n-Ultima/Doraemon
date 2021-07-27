using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Doraemon.Common.Extensions
{
    public static class EmbedExtension
    {
        public static async Task<IMessage> SendModSuccessAsync(this ISocketMessageChannel channel, string title,
            string description) //Sends the Success Message, but with the Mod Log footer.
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

        public static async Task<IMessage> SendErrorMessageAsync(this ISocketMessageChannel channel, string title,
            string description)
        {
            var e = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(title)
                .WithDescription(description);
            var message = await channel.SendMessageAsync(embed: e.Build());
            return message;
        }

        public static async Task<IMessage> SendRescindedInfractionLogMessageAsync(this ISocketMessageChannel channel,
            string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordSocketClient client,
            string infractionId = null)
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
            if (infractionType != "Warn")
            {
                builder.Append(
                    $"`{DateTimeOffset.UtcNow}`{GetEmojiForRescindedInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
            else
            {
                builder.Append(
                    $"`{DateTimeOffset.UtcNow} UTC` {GetEmojiForRescindedInfractionType("Warn")} Punishment ID `{infractionId}` was removed by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                var message = await channel.SendMessageAsync(builder.ToString());
                return message;
            }
        }

        public static async Task<IMessage> SendUpdatedInfractionLogMessageAsync(this ISocketMessageChannel channel, string caseId, string infractionType, ulong moderatorId, string reason, DiscordSocketClient client)
        {
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);
            return await channel.SendMessageAsync(
                $"`{DateTimeOffset.UtcNow} UTC` 📝 Punishment ID `{caseId}` was updated by {moderatorUser.GetFullUsername()}. Reason:\n```{reason}\n```");
            
        }
        public static async Task<IMessage> SendInfractionLogMessageAsync(this ISocketMessageChannel channel, string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordSocketClient client, string duration = null)
        {
            var subjectUser = await client.Rest.GetUserAsync(subjectId);
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);
            var builder = new StringBuilder();
            switch (infractionType)
            {
                case "Ban":
                    if (duration == null)
                    {
                        builder.Append(
                            $"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was banned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                    }
                    else
                    {
                        builder.Append(
                            $"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was banned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                    }
                    break;
                case "Mute":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was muted by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                    break;
                case "Warn":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was warned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
                case "Note":
                    if ((channel as IGuildChannel).IsPublic()) break;
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) received a note by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
                case "Kick":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was kicked by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
            }

            var message = await channel.SendMessageAsync(builder.ToString());
            return message;
        }
        private static string GetEmojiForInfractionType(string infractionType)
        {
            return infractionType switch
            {
                "Note" => "📝",
                "Warn" => "⚠️",
                "Mute" => "🔇",
                "Ban" => "🔨",
                "Kick" => "👢",
                _ => "❔"
            };
        }

        private static string GetEmojiForRescindedInfractionType(string infractionType)
        {
            return infractionType switch
            {
                "Warn" => "📝",
                "Mute" => "🔊",
                "Ban" => "🔓",
                _ => "❓"
            };
        }
    }
}