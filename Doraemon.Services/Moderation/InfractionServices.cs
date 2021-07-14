using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.Moderation
{
    public class InfractionService
    {
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly AuthorizationService _authorizationService;
        private readonly DiscordSocketClient _client;
        private readonly GuildManagementService _guildManagementService;
        private readonly InfractionRepository _infractionRepository;
        public IServiceScopeFactory _serviceScopeFactory;
        public ModerationConfiguration ModerationConfig { get; private set; } = new();
        public InfractionService(DiscordSocketClient client, AuthorizationService authorizationService,
            InfractionRepository infractionRepository, GuildManagementService guildManagementService)
        {
            _client = client;
            _authorizationService = authorizationService;
            _infractionRepository = infractionRepository;
            _guildManagementService = guildManagementService;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Creates an infraction.
        /// </summary>
        /// <param name="subjectId">The user ID that the infraction will be applied to.</param>
        /// <param name="moderatorId">The user ID applying the infraction.</param>
        /// <param name="guildId">The guild ID that the infraction is being created in.</param>
        /// <param name="type">The <see cref="InfractionType" /></param>
        /// <param name="reason">The reason for the infraction being created.</param>
        /// <param name="duration">The optional duration of the infraction.</param>
        /// <returns></returns>
        public async Task CreateInfractionAsync(ulong subjectId, ulong moderatorId, ulong guildId, InfractionType type,
            string reason, bool isEscalation, TimeSpan? duration)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionCreate);
            await _infractionRepository.CreateAsync(new InfractionCreationData
            {
                Id = DatabaseUtilities.ProduceId(),
                SubjectId = subjectId,
                ModeratorId = moderatorId,
                CreatedAt = DateTimeOffset.Now,
                Type = type,
                Reason = reason,
                Duration = duration
            });
            var guild = _client.GetGuild(guildId);
            var user = await _client.Rest.GetUserAsync(subjectId);

            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var dmChannel = await user.GetOrCreateDMChannelAsync();

            switch (type)
            {
                case InfractionType.Ban:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(),
                        _client);
                    try
                    {
                        var message = ModerationConfig.BanMessage;
                        var formattedMessage = string.Format(message, guild.Name, reason);
                        await dmChannel.SendMessageAsync(formattedMessage);
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }

                    await guild.AddBanAsync(user, 0, reason, new RequestOptions
                    {
                        AuditLogReason = reason
                    });

                    break;
                case InfractionType.Mute:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client,
                        duration.Value.Humanize());
                    var gUser = guild.GetUser(subjectId);
                    if (gUser is null) break;

                    await gUser.AddRoleAsync(mutedRole);
                    try
                    {
                        await dmChannel.SendMessageAsync(
                            $"You have been muted in {guild.Name}. Reason: {reason}\nDuration: {duration.Value.Humanize()}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }

                    break;
                case InfractionType.Note:
                    break;
                case InfractionType.Warn:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(),
                        _client);
                    try
                    {
                        await dmChannel.SendMessageAsync(
                            $"You have received a warning in {guild.Name}. Reason: {reason}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }

                    break;
                default:
                    throw new Exception($"The type: {type} threw an error. See inner stack trace for details.");
            }

            if (!isEscalation)
            {
                await CheckForMultipleInfractionsAsync(subjectId, guildId);
            }
        }

        /// <summary>
        ///     Fetches a list of infractions filtered by the type provided.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <param name="type">The type of <see cref="InfractionType" /> to filter by.</param>
        /// <returns></returns>
        public async Task<Infraction> FetchInfractionForUserAsync(ulong subjectId, ulong moderatorId,
            InfractionType type)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionView);
            return await _infractionRepository.FetchInfractionForUserByTypeAsync(subjectId, type);
        }

        /// <summary>
        ///     Fetches a list of infractions for a user.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <param name="moderatorId">The userID requesting the query.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchUserInfractionsAsync(ulong subjectId, ulong moderatorId)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionView);
            return await _infractionRepository.FetchAllUserInfractionsAsync(subjectId);
        }

        /// <summary>
        ///     Fetches a list of all timed infractions.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            return await _infractionRepository.FetchTimedInfractionsAsync();
        }

        /// <summary>
        ///     Updates the reason for the given infraction.
        /// </summary>
        /// <param name="caseId">The ID of the infraction.</param>
        /// <param name="moderatorId">The userID requesting the update.</param>
        /// <param name="newReason">The new reason that will be applied.</param>
        /// <returns></returns>
        public async Task UpdateInfractionAsync(string caseId, ulong moderatorId, string newReason)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionUpdate);
            // No need to throw an error, as it's handled in the repository.
            await _infractionRepository.UpdateAsync(caseId, newReason);
        }

        /// <summary>
        ///     Removes the infraction given.
        /// </summary>
        /// <param name="caseId">The ID of the infraction to remove.</param>
        /// <param name="reason">The reason for removing the infraction.</param>
        /// <param name="moderator">The moderator's ID who is attempting to remove the infraction.</param>
        /// <returns></returns>
        public async Task RemoveInfractionAsync(string caseId, string reason, ulong moderator)
        {
            await _authorizationService.RequireClaims(moderator, ClaimMapType.InfractionDelete);
            var infraction = await _infractionRepository.FetchInfractionByIDAsync(caseId);

            if (infraction is null) throw new InvalidOperationException("The caseID provided does not exist.");

            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var user = guild.GetUser(infraction.SubjectId);
            if (user is null)
            {
                // If the user is not in the guild, and it's a mute, AND it's already expired, we can safely remove it.
                if (infraction.Type == InfractionType.Mute)
                {
                    await _infractionRepository.DeleteAsync(infraction);
                    return;
                }

                Log.Logger.Information($"User is null, attempting to remove infraction {infraction.Id}");
            }

            var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var type = infraction.Type;

            switch (type)
            {
                case InfractionType.Mute:
                
                    await user.RemoveRoleAsync(muteRole);
                    await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId,
                        infraction.Type.ToString(), _client);
                    break;

                case InfractionType.Ban:
                    try
                    {
                        await guild.GetBanAsync(infraction.SubjectId);
                        await guild.RemoveBanAsync(infraction.SubjectId);
                    }
                    catch
                    {
                        break;
                    }
                    await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId,
                        infraction.Type.ToString(), _client);
                    break;
                case InfractionType.Note:
                    break;
                case InfractionType.Warn: 
                    await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId,
                        infraction.Type.ToString(), _client, caseId);
                    break;
            }

            await _infractionRepository.DeleteAsync(infraction);
        }

        /// <summary>
        ///     Checks if a user has matched a number of warns to trigger an escalation.
        /// </summary>
        /// <param name="userId">The user to query for.</param>
        /// <param name="guildId">The guild ID to check for.</param>
        /// <returns></returns>
        public async Task CheckForMultipleInfractionsAsync(ulong userId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            if (user is null)
                return;
            var infractions = await _infractionRepository.FetchWarnsAsync(userId);
            switch (infractions.Count())
            {
                case 1:
                    var oneWarn = await _guildManagementService.FetchPunishementConfigurationAsync(1);
                    if (oneWarn is null)
                        break;
                    await CreateInfractionAsync(userId, _client.CurrentUser.Id, guild.Id, oneWarn.Type,
                        "Automatic punishment (strike 1)", true, oneWarn.Duration);
                    break;
                case 2:
                    var twoWarns = await _guildManagementService.FetchPunishementConfigurationAsync(2);
                    if (twoWarns is null)
                        break;
                    await CreateInfractionAsync(userId, _client.CurrentUser.Id, guildId, twoWarns.Type,
                        "Automatic punishment (strike 2)", true, twoWarns.Duration);
                    break;
                case 3:
                    var threeWarns = await _guildManagementService.FetchPunishementConfigurationAsync(3);
                    if (threeWarns is null)
                        break;
                    await CreateInfractionAsync(userId, _client.CurrentUser.Id, guildId, threeWarns.Type,
                        "Automatic punishment (strike 3)", true, threeWarns.Duration);
                    break;
                case 4:
                    var fourWarns = await _guildManagementService.FetchPunishementConfigurationAsync(4);
                    if (fourWarns is null)
                        break;
                    await CreateInfractionAsync(userId, _client.CurrentUser.Id, guildId, fourWarns.Type,
                        "Automatic punishment (strike 4)", true, fourWarns.Duration);
                    break;
                case 5:
                    var fiveWarns = await _guildManagementService.FetchPunishementConfigurationAsync(5);
                    if (fiveWarns is null)
                        break;
                    await CreateInfractionAsync(userId, _client.CurrentUser.Id, guildId, fiveWarns.Type,
                        "Automatic punishment (strike 5)", true, fiveWarns.Duration);
                    break;
            }
        }
    }
}