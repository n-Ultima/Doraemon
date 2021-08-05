using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Humanizer;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Info")]
    [Description("Used for getting info on the guild or a user.")]
    public class InfoModule : DoraemonGuildModuleBase
    {
        // Get's the info for the current server.
        [Command("serverinfo")]
        [Description("Displays information for the guild that the command is ran in.")]
        public DiscordCommandResult DisplayServerInfoAsync()
        {
            var embedBuilder = new LocalEmbed()
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithColor(DColor.Gold)
                .WithThumbnailUrl(Context.Guild.GetIconUrl())
                .WithTimestamp(DateTimeOffset.UtcNow);
            var stringBuilder = new StringBuilder();
            AppendGuildInformation(stringBuilder, Context.Guild);
            AppendMemberInformation(stringBuilder, Context.Guild);
            AppendRoleInformation(stringBuilder, Context.Guild);
            embedBuilder.WithDescription(stringBuilder.ToString());
            return Response(embedBuilder);
        }

        // Show bot information
        [Command("botinfo")]
        [Description("Displays information about Doraemon.")]
        public DiscordCommandResult DisplayBotInfoAsync()
        {
            var e = new LocalEmbed()
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithTitle("Information for Doraemon#3774")
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
                .AppendLine($"Ultima#2000 - Main Developer/Maintainer")
                .AppendLine($"shift-eleven#7304 - Major contributor")
                .AppendLine($"That_One_Nerd#0001 - Major Contributor")
                .ToString();
            return Response(new LocalEmbed()
                .WithTitle("Developers of Doraemon")
                .WithDescription(builder)
                .WithFooter("Everyone listed above is equally as important, thank them for this amazing bot!")
                .WithColor(DColor.Blue));
        }
        public void AppendGuildInformation(StringBuilder stringBuilder, IGuild guild) // Declare params
        {
            stringBuilder
                .AppendLine("**\u276f Server Information**")
                .AppendLine($"ID: {guild.Id}")
                .AppendLine($"Owner: {Mention.User(guild.OwnerId)}")
                .AppendLine($"Created: {Context.Guild.CreatedAt().ToString("dd/MM/yyyy")}")
                .AppendLine();
        }

        public void AppendMemberInformation(StringBuilder stringBuilder, IGuild guild)
        {
            var members = guild.FetchMembersAsync().GetAwaiter().GetResult();
            var bots = members.Where(x => x.IsBot).ToList();
            var humans = members.Count() - bots.Count();

            stringBuilder
                .AppendLine("**❯ Member Information**")
                .AppendLine($"Total member count: {members.Count}")
                .AppendLine($"• Humans: {humans}")
                .AppendLine($"• Bots: {bots.Count}")
                .AppendLine();
        }

        public void AppendRoleInformation(StringBuilder stringBuilder, IGuild guild)
        {
            var roles = guild.Roles
                .Where(x => x.Value.Id != guild.Id)
                .Select(x => x.Value)
                .OrderByDescending(x => x.Position)
                .ThenByDescending(x => x.IsHoisted);

            stringBuilder
                .AppendLine("**\u276F Guild Roles**")
                .AppendLine(roles.Select(x => x.Mention).Humanize())
                .AppendLine();
        }
    }
}