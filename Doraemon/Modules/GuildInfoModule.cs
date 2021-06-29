using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Doraemon.Common.Extensions;
namespace Doraemon.Modules
{
    [Name("Info")]
    [Summary("Used for getting info on the guild or a user.")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public DiscordSocketClient _client;
        public InfoModule(DiscordSocketClient client)
        {
            _client = client;
        }
        // Get's the info for the current server.
        [Command("serverinfo")]
        [Alias("guildinfo")]
        [Summary("Displays information for the guild that the command is ran in.")]
        public async Task DisplayServerInfoAsync()
        {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithCurrentTimestamp();
            var stringBuilder = new StringBuilder();
            AppendGuildInformation(stringBuilder, Context.Guild);
            AppendMemberInformation(stringBuilder, Context.Guild);
            AppendRoleInformation(stringBuilder, Context.Guild);
            embedBuilder.WithDescription(stringBuilder.ToString());
            await ReplyAsync(embed: embedBuilder.Build());
        }
        // Show bot information
        [Command("botinfo")]
        [Alias("bot info", "bot information")]
        [Summary("Displays information about Doraemon.")]
        public async Task DisplayBotInfoAsync()
        {
            var e = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Information for Doraemon#3774")
                .AddField("Developers", "**Ultima#8878**", true)
                .AddField("Honorary Mention", "**That_One_Nerd#0001**", true)
                .AddField("Created At", Context.User.CreatedAt.ToString("dd/MM/yyyy"), true)
                .AddField("Language", "C#", true)
                .AddField("Version", ".NET Core 5.0", true)
                .AddField("Library", "Discord.NET 2.3.1", true)
                .AddField("Source Code", "https://github.com/n-Ultima/Doraemon", true)
                .AddField("Wiki", "https://github.com/n-Ultima/Doraemon/wiki", true)
                .AddField("Discord Support Server", "https://discord.gg/fRtEZvSv", true)
                .WithFooter("Created, maintained, and developed by Ultima#8878")
                .WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl());
            await ReplyAsync(embed: e.Build());
        }
        public void AppendGuildInformation(StringBuilder stringBuilder, SocketGuild guild) // Declare params
        {
            stringBuilder
                .AppendLine("**\u276f Server Information**")
                .AppendLine($"ID: {guild.Id}")
                .AppendLine($"Owner: {guild.Owner.Mention}")
                .AppendLine($"Created: {Context.Guild.CreatedAt.ToString("dd/MM/yyyy")}")
                .AppendLine();
        }
        public void AppendMemberInformation(StringBuilder stringBuilder, SocketGuild guild)
        {
            var members = guild.Users.Count;
            var bots = guild.Users.Count(x => x.IsBot);
            var humans = members - bots;

            stringBuilder
                .AppendLine($"**\u276F Member Information**")
                .AppendLine($"Total member count: {members}")
                .AppendLine($"• Humans: {humans}")
                .AppendLine($"• Bots: {bots}")
                .AppendLine();
        }
        public void AppendRoleInformation(StringBuilder stringBuilder, SocketGuild guild)
        {
            var roles = guild.Roles
                .Where(x => x.Id != guild.EveryoneRole.Id && x.Color != Color.Default)
                .OrderByDescending(x => x.Position)
                .ThenByDescending(x => x.IsHoisted);

            stringBuilder
                .AppendLine("**\u276F Guild PingRoles**")
                .AppendLine(string.Join(" ", roles.Select(x => x.Mention)))
                .AppendLine();
        }
    }
}
