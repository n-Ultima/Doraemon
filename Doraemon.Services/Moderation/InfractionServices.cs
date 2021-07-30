using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;
using Humanizer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.Moderation
{
    [DoraemonService]
    public class InfractionService : DiscordBotService
    {
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly AuthorizationService _authorizationService;
        private readonly GuildManagementService _guildManagementService;
        private readonly InfractionRepository _infractionRepository;
        public ModerationConfiguration ModerationConfig { get; private set; } = new();
        public InfractionService(AuthorizationService authorizationService,
            InfractionRepository infractionRepository, GuildManagementService guildManagementService)
        {
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

        public async Task CreateInfractionAsync(Snowflake subjectId, Snowflake moderatorId, Snowflake guildId, InfractionType type, string reason, bool isEscalation, TimeSpan? duration)
        {
            if (subjectId != moderatorId)
            {
                _authorizationService.RequireClaims(ClaimMapType.InfractionCreate);
            }
            
            var currentInfractionsBeforeInfraction = await _infractionRepository.FetchAllUserInfractionsAsync(subjectId);
            var check = currentInfractionsBeforeInfraction
                .Where(x => x.Type == type)
                .ToList();
            if (check.Any())
            {
                if (type == InfractionType.Ban || type == InfractionType.Mute)
                    throw new Exception($"User already has an active {type} infraction.");
            }

            var guild = Bot.GetGuild(guildId);
            var gUser = guild.GetMember(subjectId);
            var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId) as ITextChannel;
            switch (type)
            {
                case InfractionType.Note:
                    break;
                case InfractionType.Ban:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client);
                    try
                    {
                        if (gUser != null)
                        {
                            var message = ModerationConfig.BanMessage;
                            var formattedMessage = string.Format(message, guild.Name, reason);
                            await gUser.SendMessageAsync(new LocalMessage().WithContent(formattedMessage));
                        }
                    }
                    catch (RestApiException)
                    {
                        
                    }

                    await guild.CreateBanAsync(subjectId,  reason, 0, new DefaultRestRequestOptions()
                    {
                        Reason = reason
                    });
                    break;
                case InfractionType.Mute:
                    if (gUser == null)
                        throw new InvalidOperationException($"The user provided is not currently in the guild, so I can't mute them.");
                    var mutedRole = guild.Roles.Where(x => x.Name == muteRoleName).SingleOrDefault();

                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client, duration.Value.Humanize());
                    await gUser.AddRoleAsync(mutedRole);
                    try
                    {
                        var dmChannel = await gUser.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync($"You have been muted in {guild.Name}. Reason: {reason}\nDuration: {duration.Value.Humanize()}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }

                    break;
                case InfractionType.Warn:
                    if (gUser == null)
                        throw new Exception($"The user is not currently in the guild, so I can't warn them.");
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client);
                    try
                    {
                        var dmChannel = await gUser.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync(
                            $"You have received a warning in {guild.Name}. Reason: {reason}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }

                    break;

            }

            using (var transaction = await _infractionRepository.BeginCreateTransactionAsync())
            {
                await _infractionRepository.CreateAsync(new InfractionCreationData()
                {
                    Id = DatabaseUtilities.ProduceId(),
                    SubjectId = subjectId,
                    ModeratorId = moderatorId,
                    Duration = duration,
                    Type = type,
                    Reason = reason
                });
                transaction.Commit();
            }
            if (!isEscalation)
            {
                if (type == InfractionType.Warn)
                {
                    await CheckForMultipleInfractionsAsync(subjectId, guild.Id);
                }
            }
        }
        /// <summary>
        ///     Fetches a list of infractions for a user.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchUserInfractionsAsync(ulong subjectId)
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionView);
            return await _infractionRepository.FetchAllUserInfractionsAsync(subjectId);
        }

        /// <summary>
        ///     Fetches a list of all timed infractions.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionView);
            using (var transaction = await _infractionRepository.BeginCreateTransactionAsync())
            {
                var results = await _infractionRepository.FetchTimedInfractionsAsync();
                transaction.Commit();
                return results;
            }
        }

        /// <summary>
        ///     Updates the reason for the given infraction.
        /// </summary>
        /// <param name="caseId">The ID of the infraction.</param>
        /// <param name="newReason">The new reason that will be applied.</param>
        /// <returns></returns>
        public async Task UpdateInfractionAsync(string caseId, string newReason)
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionUpdate);
            var infractionToUpdate = await _infractionRepository.FetchInfractionByIdAsync(caseId);
            if (infractionToUpdate == null)
                throw new Exception($"The infraction ID provided does not currently exist.");
            // No need to throw an error, as it's handled in the repository.
            await _infractionRepository.UpdateAsync(infractionToUpdate.Id, newReason);
            var modLogChannel = _client.GetGuild(DoraemonConfig.MainGuildId).GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            await modLogChannel.SendUpdatedInfractionLogMessageAsync(infractionToUpdate.Id, infractionToUpdate.Type.ToString(), _authorizationService.CurrentUser, newReason, _client);
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
            _authorizationService.RequireClaims(ClaimMapType.InfractionDelete);
            var infraction = await _infractionRepository.FetchInfractionByIdAsync(caseId);

            if (infraction is null) throw new ArgumentException("The caseID provided does not exist.");

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

            using (var transaction = await _infractionRepository.BeginCreateTransactionAsync())
            {
                await _infractionRepository.DeleteAsync(infraction);
                transaction.Commit();  
            } 
        }

        /// <summary>
        ///     Checks if a user has matched a number of warns to trigger an escalation.
        /// </summary>
        /// <param name="userId">The user to query for.</param>
        /// <param name="guildId">The guild ID to check for.</param>
        /// <returns></returns>
        private async Task CheckForMultipleInfractionsAsync(ulong userId, ulong guildId)
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