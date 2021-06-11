using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Doraemon;
using Doraemon.Common;

namespace Doraemon.Data.Services
{
    public class GuildManagementService
    {
        public DiscordSocketClient _client;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public bool RaidModeEnabled = false;
        public GuildManagementService(DiscordSocketClient client)
        {
            _client = client;
        }
        /// <summary>
        /// Enables raid mode, preventing user joins.
        /// </summary>
        /// <param name="moderatorId"></param>
        /// <param name="guildId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task EnableRaidModeAsync(ulong moderatorId, ulong guildId, string reason)
        {
            RaidModeEnabled = true;
            var guild = _client.GetGuild(guildId);
            var moderator = guild.GetUser(moderatorId);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var embed = new EmbedBuilder()
                .WithTitle("Raid Mode Log")
                .WithDescription($"Raid mode was enabled by {moderator.Mention}\nReason: {reason}")
                .WithFooter("Use \"!raidmode disable\" to disable raidmode. ")
                .Build();
            await modLog.SendMessageAsync(embed: embed);
        }
        /// <summary>
        /// Disables raid mode, allowing user joins.
        /// </summary>
        /// <param name="moderatorId"></param>
        /// <param name="guildId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task DisableRaidModeAsync(ulong moderatorId, ulong guildId, string reason = null)
        {
            RaidModeEnabled = false;
            var guild = _client.GetGuild(guildId);
            var moderator = guild.GetUser(moderatorId);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var embed = new EmbedBuilder()
                .WithTitle($"Raid Mode Log")
                .WithDescription($"Raid mode was disabled by {moderator.Mention}\nReason: {reason ?? "Not specified"}")
                .WithFooter($"Use \"!raidmode enable\" to enable raidmode.")
                .Build();
            await modLog.SendMessageAsync(embed: embed);
        }
        /// <summary>
        /// Returns if raid mode is enabled or disabled.
        /// </summary>
        /// <returns></returns>
        public async Task<string> FetchCurrentRaidModeAsync()
        {
            if (RaidModeEnabled)
            {
                return "Enabled";
            }
            return "Disabled";
        }
    }
}
