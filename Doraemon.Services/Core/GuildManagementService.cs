using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Common;
using Discord;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.Core
{
    public class GuildManagementService
    {
        public DiscordSocketClient _client;
        public DoraemonContext _doraemonContext;
        public AuthorizationService _authorizationService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public bool RaidModeEnabled = false;
        public GuildManagementService(DiscordSocketClient client, DoraemonContext doraemonContext, AuthorizationService authorizationService)
        {
            _client = client;
            _doraemonContext = doraemonContext;
            _authorizationService = authorizationService;
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
        public string FetchCurrentRaidModeAsync()
        {
            if (RaidModeEnabled)
            {
                return "Enabled";
            }
            return "Disabled";
        }

        /// <summary>
        /// Adds a guild to the whitelist, allowing invites to it to remain un-moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="guildName">The name of the guild.</param>
        /// <param name="requestorId">The user requesting that the guild be whitelisted.</param>
        /// <returns></returns>
        public async Task AddWhitelistedGuildAsync(string guildId, string guildName, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
            var g = await _doraemonContext
                .Set<Guild>()
                .Where(x => x.Id == guildId)
                .SingleOrDefaultAsync();
            if (g is not null)
            {
                throw new ArgumentException("That guild ID is already present on the whitelist.");
            }
            _doraemonContext.Guilds.Add(new Guild { Id = guildId, Name = guildName });
            await _doraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Blacklists a guild, causing invites to be moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="requestorId">The user requesting the blacklist.</param>
        /// <returns></returns>
        public async Task BlacklistGuildAsync(string guildId, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
            var g = await _doraemonContext
                .Set<Guild>()
                .Where(x => x.Id == guildId)
                .SingleOrDefaultAsync();
            if (g is null)
            {
                throw new ArgumentException("That guild ID is not present on the whitelist.");
            }
            _doraemonContext.Guilds.Remove(g);
            await _doraemonContext.SaveChangesAsync();
        }

        public async Task<List<Guild>> FetchAllWhitelistedGuildsAsync()
        {
            return await _doraemonContext.Guilds.AsQueryable().OrderBy(x => x.Name).ToListAsync();
        }
    }
}
