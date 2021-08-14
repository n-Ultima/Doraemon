using System.Text;
using Disqord;
using Disqord.Bot;
using Doraemon.Common.Extensions;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Botinfo")]
    [Description("Provides commands for information about the bot.")]
    public class BotInfoModule : DoraemonGuildModuleBase
    {
        
        // Show bot information
        [Command("botinfo")]
        [Description("Displays information about Doraemon.")]
        public DiscordCommandResult DisplayBotInfoAsync()
        {
            var e = new LocalEmbed()
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithTitle($"Information for {Context.Bot.CurrentUser.Tag}")
                .AddField("Created At", Context.Author.CreatedAt().ToString("dd/MM/yyyy"), true)
                .AddField("Language", "C#", true)
                .AddField("Version", ".NET 5.0", true)
                .AddField("Library", "Disqord Nightly", true)
                .AddField("Source Code", "https://github.com/n-Ultima/Doraemon", true)
                .AddField("Wiki", "https://github.com/n-Ultima/Doraemon/wiki", true)
                .AddField("Discord Support Server", "http://www.ultima.one/discord", true)
                .WithFooter("Created, maintained, and developed by Ultima#2000")
                .WithThumbnailUrl(Bot.CurrentUser.GetAvatarUrl());
            return Response(e);
        }

        [Command("devs", "developers")]
        [Description("Shows off the developers of Doraemon.")]
        public DiscordCommandResult ShowOffDevs()
        {
            var builder = new StringBuilder()
                .AppendLine($"**Ultima#2000** - Main Developer/Maintainer")
                .AppendLine($"**shift-eleven#7304** - Major contributor")
                .AppendLine($"**That_One_Nerd#0001** - Major Contributor")
                .ToString();
            return Response(new LocalEmbed()
                .WithTitle("Developers of Doraemon")
                .WithDescription(builder)
                .WithFooter("Everyone listed above is equally as important, thank them for this amazing bot!")
                .WithColor(DColor.Blue));
        }
    }
}