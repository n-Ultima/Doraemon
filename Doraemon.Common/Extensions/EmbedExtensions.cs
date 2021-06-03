using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

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
        public static async Task<IMessage> SendInfractionLogMessageAsync(this ISocketMessageChannel channel, string reason, ulong moderator, ulong subject, string infractionType)
        {
            var e = new EmbedBuilder()
                .WithTitle("Infraction Log")
                .AddField($"Moderator", $"<@{moderator}>")
                .AddField($"User", $"<@{subject}>")
                .AddField($"Infraction Type", infractionType)
                .AddField($"Reason", reason);
            var message = await channel.SendMessageAsync(embed: e.Build());
            return message;
        }
        public static async Task<IDMChannel> SendBanDMAsync(this IDMChannel channel, string title, string description)
        {
            var e = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Red)
                .WithDescription(description + " [here](https://discord.gg/Qzk9BCvTGm)");
            var message = await channel.SendMessageAsync(embed: e.Build());
            return channel;
        }
    }
}

