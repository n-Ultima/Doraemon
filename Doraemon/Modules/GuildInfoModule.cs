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
        [Command("serverinfo", "guildinfo")]
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