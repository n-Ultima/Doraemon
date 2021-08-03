using System;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Doraemon.Common.Extensions
{
    public static class EmbedExtension
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public static async Task<IMessage> SendRescindedInfractionLogMessageAsync(this CachedGuildChannel channel,
            string reason, Snowflake moderatorId, Snowflake subjectId, string infractionType, DiscordBotBase client,
            string infractionId = null)
        {
            string format;
            var subjectUser = await client.FetchUserAsync(subjectId);
            var moderatorUser = await client.FetchUserAsync(moderatorId);

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
                var embed = new LocalEmbed()
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithColor(Color.Green)
                    .WithAuthor(client.CurrentUser);
                if (infractionType != "Warn")
                {
                    embed.WithTitle($"{GetEmojiForRescindedInfractionType("Warn")} Punishment ID Removed!");
                    embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                }
                else
                {
                    embed.WithTitle($"{GetEmojiForRescindedInfractionType(infractionType)} User {format}");
                    embed.WithDescription($"Punishment ID `{infractionId}` was removed by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                }

                return await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithEmbeds(embed));
            }
            else
            {
                var builder = new StringBuilder();
                if (infractionType != "Warn")
                {
                    builder.Append(
                        $"`{DateTimeOffset.UtcNow}`{GetEmojiForRescindedInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was {format} by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                    var message = await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithContent(builder.ToString()));
                    return message;
                }
                else
                {
                    builder.Append(
                        $"`{DateTimeOffset.UtcNow} UTC` {GetEmojiForRescindedInfractionType("Warn")} Punishment ID `{infractionId}` was removed by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason ?? "Not specified"}```");
                    var message = await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithContent(builder.ToString()));
                    return message;
                }   
            }
        }

        public static async Task<IMessage> SendUpdatedInfractionLogMessageAsync(this CachedGuildChannel channel, string caseId, string infractionType, ulong moderatorId, string reason, DiscordBotBase client)
        {
            var moderatorUser = await client.FetchUserAsync(moderatorId);
            if (DoraemonConfig.LogConfiguration.EmbedOrText == "EMBED")
            {
                var embed = new LocalEmbed()
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithColor(Color.Green)
                    .WithDescription($"📝 Punishment ID `{caseId}` was updated by {moderatorUser.Tag}. Reason:\n```{reason}\n```")
                    .WithAuthor(client.CurrentUser);
                return await (channel as ITextChannel).SendMessageAsync(new LocalMessage()
                    .WithEmbeds());
            }
            return await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithContent(
                $"`{DateTimeOffset.UtcNow} UTC` 📝 Punishment ID `{caseId}` was updated by {moderatorUser.Tag}. Reason:\n```{reason}\n```"));
            
        }
        public static async Task<IMessage> SendInfractionLogMessageAsync(this CachedGuildChannel channel, string reason, ulong moderatorId, ulong subjectId, string infractionType, DiscordBotBase client, string duration = null)
        {
            var subjectUser = await client.FetchUserAsync(subjectId);
            var moderatorUser = await client.FetchUserAsync(moderatorId);
            if (DoraemonConfig.LogConfiguration.EmbedOrText == "EMBED")
            {
                var embed = new LocalEmbed()
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithTitle($"{GetEmojiForInfractionType(infractionType)} User {GetFormat(infractionType)}!")
                    .WithColor(Color.Green)
                    .WithAuthor(client.CurrentUser);
                switch (infractionType)
                {
                    case "Ban":
                        if (duration == null)
                        {
                            embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was banned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                        }
                        else
                        {
                            embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was banned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                        }

                        break;
                    case "Mute":
                        embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was muted by **{moderatorUser.Tag}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                        break;
                    case "Warn":
                        embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was warned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                    case "Note":
                        embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) received a note by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                    case "Kick":
                        embed.WithDescription($"**{subjectUser.Tag}**(`{subjectUser.Id}`) was kicked by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                        break;
                        
                }

                return await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithEmbeds(embed));
            }
            var builder = new StringBuilder();
            switch (infractionType)
            {
                case "Ban":
                    if (duration == null)
                    {
                        builder.Append(
                            $"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was banned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`). Reason:\n```{reason}```");
                    }
                    else
                    {
                        builder.Append(
                            $"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was banned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                    }
                    break;
                case "Mute":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was muted by **{moderatorUser.Tag}**(`{moderatorUser.Id}`) for {duration}. Reason:\n```{reason}```");
                    break;
                case "Warn":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was warned by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
                case "Note":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) received a note by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
                case "Kick":
                    builder.Append($"`{DateTimeOffset.UtcNow} UTC`{GetEmojiForInfractionType(infractionType)} **{subjectUser.Tag}**(`{subjectUser.Id}`) was kicked by **{moderatorUser.Tag}**(`{moderatorUser.Id}`).Reason:\n```{reason}```");
                    break;
            }

            var message = await (channel as ITextChannel).SendMessageAsync(new LocalMessage().WithContent(builder.ToString()));
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