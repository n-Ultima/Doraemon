using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class GuildManagementService : DoraemonBotService
    {
        private readonly AuthorizationService _authorizationService;
        public bool RaidModeEnabled;

        public GuildManagementService(AuthorizationService authorizationService, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _authorizationService = authorizationService;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Enables raid mode, preventing user joins.
        /// </summary>
        /// <param name="moderatorId">The ID value of the user requesting the action.</param>
        /// <param name="guildId">The ID value of the guild that raidmode is being enabled in.</param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task EnableRaidModeAsync(Snowflake moderatorId, Snowflake guildId, string reason)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildRaidModeManage);
            RaidModeEnabled = true;
            var guild = Bot.GetGuild(guildId);
            var moderator = guild.GetMember(moderatorId);
            var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId) as ITextChannel;
            var embed = new LocalEmbed()
                .WithTitle("Raid Mode Log")
                .WithDescription($"Raid mode was enabled by {moderator.Mention}\nReason: {reason}")
                .WithFooter("Use \"!raidmode disable\" to disable raidmode. ");
            await modLog.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
        }

        /// <summary>
        ///     Disables raid mode, allowing user joins.
        /// </summary>
        /// <param name="moderatorId">The ID value of the user requesting this action.</param>
        /// <param name="guildId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task DisableRaidModeAsync(Snowflake moderatorId, Snowflake guildId, string reason = null)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildRaidModeManage);
            RaidModeEnabled = false;
            var guild = Bot.GetGuild(guildId);
            var moderator = guild.GetMember(moderatorId);
            var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId) as ITextChannel;
            var embed = new LocalEmbed()
                .WithTitle("Raid Mode Log")
                .WithDescription($"Raid mode was disabled by {moderator.Mention}\nReason: {reason ?? "Not specified"}")
                .WithFooter("Use \"!raidmode enable\" to enable raidmode.");
            await modLog.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
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
        /// <returns></returns>
        public async Task AddWhitelistedGuildAsync(string guildId, string guildName)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildInviteWhitelistManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildRepository = scope.ServiceProvider.GetRequiredService<GuildRepository>();
                var g = await guildRepository.FetchGuildAsync(guildId);
                if (g is not null) throw new ArgumentException("That guild ID is already present on the whitelist.");
                await guildRepository.CreateAsync(new GuildCreationData
                {
                    Id = guildId,
                    Name = guildName
                });   
            }
        }

        /// <summary>
        ///     Blacklists a guild, causing invites to be moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <returns></returns>
        public async Task BlacklistGuildAsync(string guildId)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildInviteWhitelistManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildRepository = scope.ServiceProvider.GetRequiredService<GuildRepository>();
                var g = await guildRepository.FetchGuildAsync(guildId);
                if (g is null) throw new ArgumentException("That guild ID is not present on the whitelist.");
                await guildRepository.DeleteAsync(g);   
            }
        }

        /// <summary>
        ///     Fetches guilds present on the whitelist.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Guild}" />.</returns>
        public async Task<IEnumerable<Guild>> FetchAllWhitelistedGuildsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildRepository = scope.ServiceProvider.GetRequiredService<GuildRepository>();
                return await guildRepository.FetchAllWhitelistedGuildsAsync();
            }
        }

        /// <summary>
        /// Adds a punishment configuration to the guild specified.
        /// </summary>
        /// <param name="requestorId">The ID value of the user requesting this action.</param>
        /// <param name="numberOfInfractions">The number of infractions that a user must gain for this configuration to trigger.</param>
        /// <param name="type">The type of infraction that should be applied when the <see cref="numberOfInfractions"/> is reached.</param>
        /// <param name="duration">The optional duration that the punishment will be applied.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task AddPunishmentConfigurationAsync(int numberOfInfractions, InfractionType type,
            TimeSpan? duration)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildPunishmentEscalationManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var punishmentEscalationConfigurationRepository = scope.ServiceProvider.GetRequiredService<PunishmentEscalationConfigurationRepository>();
                var check = await punishmentEscalationConfigurationRepository.FetchAsync(numberOfInfractions, type);
                if (check is not null)
                {
                    throw new ArgumentException($"This punishment escalation is already configured.");
                }

                var check2 = await punishmentEscalationConfigurationRepository.FetchAsync(numberOfInfractions);
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
                    throw new IndexOutOfRangeException($"Please provide a number of infractions less than 5.");
                }

                if (type is InfractionType.Warn && duration.HasValue)
                    throw new InvalidOperationException($"Warns cannot have a duration.");

                await punishmentEscalationConfigurationRepository.CreateAsync(new PunishmentEscalationConfigurationCreationData()
                {
                    NumberOfInfractionsToTrigger = numberOfInfractions,
                    Type = type,
                    Duration = duration
                });   
            }
        }

        /// <summary>
        /// Fetches a punishment configuration that will trigger based on the number provided.
        /// </summary>
        /// <param name="num">The number of warns.</param>
        /// <returns>A <see cref="PunishmentEscalationConfiguration"/> with the provided number.</returns>
        public async Task<PunishmentEscalationConfiguration> FetchPunishementConfigurationAsync(int num)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var punishmentEscalationConfigurationRepository = scope.ServiceProvider.GetRequiredService<PunishmentEscalationConfigurationRepository>();
                return await punishmentEscalationConfigurationRepository.FetchAsync(num);
            }
        }

        /// <summary>
        ///     Modifies an already-existing punishment configuration.
        /// </summary>
        /// <param name="punishment">The number of punishments required for this configuration to trigger.</param>
        /// <param name="updatedType">The optional updated type of infraction to be applied.</param>
        /// <param name="updatedDuration">The optional updated duration of the value.</param>
        /// <exception cref="ArgumentNullException">Thrown if the config attempting to be edited is not present currently.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a duration is attempted to be applied to a <see cref="InfractionType.Warn"/></exception>
        public async Task ModifyPunishmentConfigurationAsync(int punishment, InfractionType? updatedType, TimeSpan? updatedDuration)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildPunishmentEscalationManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var punishmentEscalationConfigurationRepository = scope.ServiceProvider.GetRequiredService<PunishmentEscalationConfigurationRepository>();
                var configToEdit = await punishmentEscalationConfigurationRepository.FetchAsync(punishment);

                if (configToEdit is null)
                    throw new ArgumentNullException($"The punishement count provided does not have a configuration.");
                if (configToEdit.Type == InfractionType.Warn && updatedDuration.HasValue)
                    throw new ArgumentException($"Warns cannot have durations.");
                if (updatedDuration.HasValue && updatedType.HasValue)
                {
                    await punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, updatedType,
                        updatedDuration);
                }

                if (updatedDuration.HasValue && !updatedType.HasValue)
                {
                    await punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, null, updatedDuration);
                }

                if (!updatedDuration.HasValue && updatedType.HasValue)
                {
                    await punishmentEscalationConfigurationRepository.UpdateAsync(configToEdit, updatedType.Value, null);
                }
            }
        }

        /// <summary>
        /// Deletes an existing punishment configuration.
        /// </summary>
        /// <param name="punishement">The number of punishments needed to trigger the configuration.</param>
        /// <exception cref="ArgumentException">Thrown if the <see cref="punishement"/> provided does not have a configuration value set.</exception>
        public async Task DeletePunishmentConfigurationAsync(int punishement)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildPunishmentEscalationManage);

            using (var scope = ServiceProvider.CreateScope())
            {
                var punishmentEscalationConfigurationRepository = scope.ServiceProvider.GetRequiredService<PunishmentEscalationConfigurationRepository>();
                var configToDelete = await punishmentEscalationConfigurationRepository.FetchAsync(punishement);
                if (configToDelete == null)
                    throw new ArgumentException($"The punishment count provided does not have a configuration set.");
                await punishmentEscalationConfigurationRepository.DeleteAsync(configToDelete);   
            }
        }
    }
}