using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.GatewayEventHandlers;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Guilds")]
    [Description("Adds commands for blacklisting and whitelisting guilds.")]
    [Group("guild", "guilds")] 
    public class GuildInviteModule : DoraemonGuildModuleBase
    {
        private readonly GuildManagementService _guildService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public GuildInviteModule
        (
            GuildManagementService guildService
        )
        {
            _guildService = guildService;
        }

        [Command("whitelist")]
        [RequireClaims(ClaimMapType.GuildInviteWhitelistManage)]
        [Description("Adds a guild to the list of guilds that will not be filtered by Auto Moderation system.")]
        public async Task<DiscordCommandResult> WhitelistGuildAsync(
            [Description("The ID of the guild to whitelist.")]
                string guildId,
            [Description("The name of the guild")]
                [Remainder] string guildName)
        {
            await _guildService.AddWhitelistedGuildAsync(guildId, guildName);
            return Confirmation();
        }

        [Command("blacklist")]
        [RequireClaims(ClaimMapType.GuildInviteWhitelistManage)]
        [Description("Blacklists a guild, causing all invites to be moderated.")]
        public async Task<DiscordCommandResult> BlacklistGuildAsync(
            [Description("The ID of the guild to be removed from the whitelist.")]
                string guildId)
        {
            await _guildService.BlacklistGuildAsync(guildId);
            return Confirmation();
        }

        [Command("", "list")]
        [Priority(10)]
        [Description("Lists all whitelisted guilds.")]
        public async Task<DiscordCommandResult> ListWhitelistedGuildsAsync()
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
                .WithFooter($"Use \"{DoraemonConfig.Prefix}help guilds\" to view available commands relating to guilds!");
            return Response(embed);
        }
    }
}