using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Guilds")]
    [Description("Adds commands for blacklisting and whitelisting guilds.")]
    [Group("guild", "guilds")] 
    public class GuildInviteModule : DiscordGuildModuleBase
    {
        private readonly AutoModeration _autoModeration;
        private readonly GuildManagementService _guildService;

        public GuildInviteModule
        (
            GuildManagementService guildService,
            AutoModeration autoModeration
        )
        {
            _autoModeration = autoModeration;
            _guildService = guildService;
        }

        [Command("whitelist")]
        [RequireGuildOwner]
        [Description("Adds a guild to the list of guilds that will not be filtered by Auto Moderation system.")]
        public async Task WhitelistGuildAsync(
            [Description("The ID of the guild to whitelist.")]
                string guildId,
            [Description("The name of the guild")]
                [Remainder] string guildName)
        {
            await _guildService.AddWhitelistedGuildAsync(guildId, guildName);
            await Context.AddConfirmationAsync();
        }

        [Command("blacklist")]
        [Description("Blacklists a guild, causing all invites to be moderated.")]
        public async Task BlacklistGuildAsync(
            [Description("The ID of the guild to be removed from the whitelist.")]
                string guildId)
        {
            await _guildService.BlacklistGuildAsync(guildId);
            await Context.AddConfirmationAsync();
        }

        [Command("", "list")]
        [Priority(10)]
        [Description("Lists all whitelisted guilds.")]
        public async Task ListWhitelistedGuildsAsync()
        {
            var builder = new StringBuilder();
            foreach (var guild in await _guildService.FetchAllWhitelistedGuildsAsync())
            {
                builder.AppendLine($"**Guild Name: {guild.Name}**");
                builder.AppendLine($"**Guild ID:** `{guild.Id}`");
                builder.AppendLine();
            }

            var embed = new LocalEmbed()
                .WithTitle("Whitelisted Guilds")
                .WithDescription(builder.ToString())
                .WithFooter("Use \"!help guilds\" to view available commands relating to guilds!");
            await Context.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
        }
    }
}