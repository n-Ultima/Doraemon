using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Doraemon.Common.Extensions
{
    public static class EmbedExtension
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
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
            if (DoraemonConfig.LogConfiguration.EmbedOrText == "EMBED")
            {
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .WithAuthor(client.CurrentUser);
                if (infractionType != "Warn")
                {
                    embed.WithTitle($"{GetEmojiForRescindedInfractionType("Warn")} Punishment ID Removed!");
                    embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                }
                else
                {
                    embed.WithTitle($"{GetEmojiForRescindedInfractionType(infractionType)} User {format}");
                    embed.WithDescription($"Punishment ID `{infractionId}` was removed by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                }

                return await channel.SendMessageAsync(embed: embed.Build());
            }
            else
            {
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
        }

        public static async Task<IMessage> SendUpdatedInfractionLogMessageAsync(this ISocketMessageChannel channel, string caseId, string infractionType, ulong moderatorId, string reason, DiscordSocketClient client)
        {
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);
            if (DoraemonConfig.LogConfiguration.EmbedOrText == "EMBED")
            {
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green)
                    .WithDescription($"📝 Punishment ID `{caseId}` was updated by {moderatorUser.GetFullUsername()}. Reason:\n```{reason}\n```")
                    .WithAuthor(client.CurrentUser)
                    .Build();
                return await channel.SendMessageAsync(embed: embed);
            }
            return await channel.SendMessageAsync(
                $"`{DateTimeOffset.UtcNow} UTC` 📝 Punishment ID `{caseId}` was updated by {moderatorUser.GetFullUsername()}. Reason:\n```{reason}\n```");
            
        }
        public static async Task<IMessage> SendInfractionLogMessageAsync(this ISocketMessageChannel channel, string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordSocketClient client, string duration = null)
        {
            var subjectUser = await client.Rest.GetUserAsync(subjectId);
            var moderatorUser = await client.Rest.GetUserAsync(moderatorId);
            if (DoraemonConfig.LogConfiguration.EmbedOrText == "EMBED")
            {
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle($"{GetEmojiForInfractionType(infractionType)} User {GetFormat(infractionType)}!")
                    .WithColor(Color.Green)
                    .WithAuthor(client.CurrentUser);
                switch (infractionType)
                {
                    case "Ban":
                        if (duration == null)
                        {
                            embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was banned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                        }
                        else
                        {
                            embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was banned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                        }

                        break;
                    case "Mute":
                        embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was muted by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                        break;
                    case "Warn":
                        embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was warned by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                    case "Note":
                        if ((channel as IGuildChannel).IsPublic()) break;
                        embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) received a note by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                    case "Kick":
                        embed.WithDescription($"**{subjectUser.GetFullUsername()}**(`{subjectUser.Id}`) was kicked by **{moderatorUser.GetFullUsername()}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                        
                }

                return await channel.SendMessageAsync(embed: embed.Build());
            }
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

        private static string GetFormat(string input)
        {
            return input switch
            {
                "Warn" => "warned",
                "Mute" => "muted",
                "Ban" => "banned",
                "Kick" => "kicked",
                _ => "Undefined"
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