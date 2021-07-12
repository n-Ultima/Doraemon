using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    public class GuildManagementService
    {
        public AuthorizationService _authorizationService;
        public DiscordSocketClient _client;
        public GuildRepository _guildRepository;
        private readonly PunishmentEscalationConfigurationRepository _punishmentEscalationConfigurationRepository;
        public bool RaidModeEnabled;

        public GuildManagementService(DiscordSocketClient client, AuthorizationService authorizationService,
            GuildRepository guildRepository, PunishmentEscalationConfigurationRepository punishmentEscalationConfigurationRepository)
        {
            _guildRepository = guildRepository;
            _client = client;
            _authorizationService = authorizationService;
            _punishmentEscalationConfigurationRepository = punishmentEscalationConfigurationRepository;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Enables raid mode, preventing user joins.
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
        ///     Disables raid mode, allowing user joins.
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
                .WithTitle("Raid Mode Log")
                .WithDescription($"Raid mode was disabled by {moderator.Mention}\nReason: {reason ?? "Not specified"}")
                .WithFooter("Use \"!raidmode enable\" to enable raidmode.")
                .Build();
            await modLog.SendMessageAsync(embed: embed);
        }

        /// <summary>
        ///     Returns if raid mode is enabled or disabled.
        /// </summary>
        /// <returns></returns>
        public string FetchCurrentRaidModeAsync()
        {
            if (RaidModeEnabled) return "Enabled";
            return "Disabled";
        }

        /// <summary>
        ///     Adds a guild to the whitelist, allowing invites to it to remain un-moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="guildName">The name of the guild.</param>
        /// <param name="requestorId">The user requesting that the guild be whitelisted.</param>
        /// <returns></returns>
        public async Task AddWhitelistedGuildAsync(string guildId, string guildName, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
            var g = await _guildRepository.FetchGuildAsync(guildId);
            if (g is not null) throw new ArgumentException("That guild ID is already present on the whitelist.");
            await _guildRepository.CreateAsync(new GuildCreationData
            {
                Id = guildId,
                Name = guildName
            });
        }

        /// <summary>
        ///     Blacklists a guild, causing invites to be moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="requestorId">The user requesting the blacklist.</param>
        /// <returns></returns>
        public async Task BlacklistGuildAsync(string guildId, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
            var g = await _guildRepository.FetchGuildAsync(guildId);
            if (g is null) throw new ArgumentException("That guild ID is not present on the whitelist.");
            await _guildRepository.DeleteAsync(g);
        }

        /// <summary>
        ///     Fetches guilds present on the whitelist.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Guild}" />.</returns>
        public async Task<IEnumerable<Guild>> FetchAllWhitelistedGuildsAsync()
        {
            return await _guildRepository.FetchAllWhitelistedGuildsAsync();
        }

        public async Task AddPunishmentConfigurationAsync(ulong requestorId, int numberOfInfractions, InfractionType type,
            TimeSpan? duration)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.AuthorizationManage);
            var check = await _punishmentEscalationConfigurationRepository.FetchAsync(numberOfInfractions, type);
            if (check is not null)
            {
                throw new InvalidOperationException($"This punishment escalation is already configured.");
            }

            var check2 = await _punishmentEscalationConfigurationRepository.FetchAsync(numberOfInfractions);
            if (check2 is not null)
            {
                if (check2.Type == type)
                {
                    throw new InvalidOperationException($"There is already an escalation set for this amount of warns.");
                }
                if (check2.Type != type)
                {
                    throw new InvalidOperationException(
                        $"There is an already existing configuration for this amount of warns with a different punishement.");
                }
            }
            if (numberOfInfractions > 5)
            {
                throw new InvalidOperationException($"Please provide a number of infractions less than 5.");
            }

            if (type is InfractionType.Warn && duration.HasValue)
                throw new InvalidOperationException($"Warns cannot have a duration.");
            await _punishmentEscalationConfigurationRepository.CreateAsync(
                new PunishmentEscalationConfigurationCreationData()
                {
                    NumberOfInfractionsToTrigger = numberOfInfractions,
                    Type = type,
                    Duration = duration
                });
        }

        public async Task<PunishmentEscalationConfiguration> FetchPunishementConfigurationAsync(int num)
        {
            return await _punishmentEscalationConfigurationRepository.FetchAsync(num);
        }

        public async Task ModifyPunishmentConfigurationAsync(ulong requestorId, int punishment, InfractionType? updatedType, TimeSpan? updatedDuration)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.AuthorizationManage);
            var configToEdit = await _punishmentEscalationConfigurationRepository.FetchAsync(punishment);

            if (configToEdit is null)
                throw new ArgumentNullException($"The punishement count provided does not have a configuration.");
            if (configToEdit.Type == InfractionType.Warn && updatedDuration.HasValue)
                throw new InvalidOperationException($"Warns cannot have durations.");
            if (updatedDuration.HasValue && updatedType.HasValue)
            {
                await _punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, updatedType,
                    updatedDuration);
            }

            if (updatedDuration.HasValue && !updatedType.HasValue)
            {
                await _punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, null, updatedDuration);
            }

            if (!updatedDuration.HasValue && updatedType.HasValue)
            {
                await _punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, updatedType.Value, null);
            }
            

        }
    }
}