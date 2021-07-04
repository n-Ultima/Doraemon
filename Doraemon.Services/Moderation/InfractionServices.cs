using System;
using System.Collections.Generic;
using System.Linq;
using Doraemon.Data.Models.Core;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data;
using Doraemon.Data.Models.Moderation;
using Discord.WebSocket;
using Humanizer;
using Doraemon.Common.Utilities;
using Discord;
using Doraemon.Common;
using Discord.Net;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Doraemon.Services.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Moderation
{
    public class InfractionService
    {
        public IServiceScopeFactory _serviceScopeFactory;
        private readonly DiscordSocketClient _client;
        private readonly AuthorizationService _authorizationService;
        private readonly InfractionRepository _infractionRepository;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public InfractionService(DoraemonContext doraemonContext, DiscordSocketClient client, AuthorizationService authorizationService, InfractionRepository infractionRepository)
        {
            _client = client;
            _authorizationService = authorizationService;
            _infractionRepository = infractionRepository;
        }

        /// <summary>
        /// Creates an infraction.
        /// </summary>
        /// <param name="subjectId">The user ID that the infraction will be applied to.</param>
        /// <param name="moderatorId">The user ID applying the infraction.</param>
        /// <param name="guildId">The guild ID that the infraction is being created in.</param>
        /// <param name="type">The <see cref="InfractionType"/></param>
        /// <param name="reason">The reason for the infraction being created.</param>
        /// <param name="duration">The optional duration of the infraction.</param>
        /// <returns></returns>
        public async Task CreateInfractionAsync(ulong subjectId, ulong moderatorId, ulong guildId, InfractionType type, string reason, TimeSpan? duration)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionCreate);
            await _infractionRepository.CreateAsync(new InfractionCreationData()
            {
                Id = DatabaseUtilities.ProduceId(),
                SubjectId = subjectId,
                ModeratorId = moderatorId,
                CreatedAt = DateTimeOffset.Now,
                Type = type,
                Reason = reason,
                Duration = duration
            });
            var currentInfractions = await _infractionRepository.FetchNormalizedInfractionsAsync(subjectId);
            var guild = _client.GetGuild(guildId);
            var user = await _client.Rest.GetUserAsync(subjectId);

            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var dmChannel = await user.GetOrCreateDMChannelAsync();

            switch (type)
            {
                case InfractionType.Ban:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client);
                    try
                    {
                        await dmChannel.SendMessageAsync($"You have been banned from {guild.Name}. Reason: {reason}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }
                    await guild.AddBanAsync(user, 0, reason, options: new RequestOptions()
                    {
                        AuditLogReason = reason
                    });

                    break;
                case (InfractionType.Mute):
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client, duration.Value.Humanize());
                    var gUser = guild.GetUser(subjectId);
                    if (gUser is null)
                    {
                        break;
                    }
                    await gUser.AddRoleAsync(mutedRole);
                    try
                    {
                        await dmChannel.SendMessageAsync($"You have been muted in {guild.Name}. Reason: {reason}\nDuration: {duration.Value.Humanize()}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync($"I was unable to DM the user for the above infraction.");
                    }
                    break;
                case InfractionType.Note:
                    break;
                case InfractionType.Warn:
                    await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), _client);
                    try
                    {
                        await dmChannel.SendMessageAsync($"You have received a warning in {guild.Name}. Reason: {reason}");
                    }
                    catch (HttpException)
                    {
                        await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                    }
                    break;
                default:
                    throw new Exception($"The type: {type} threw an error. See inner stack trace for details.");
            }
        }

        /// <summary>
        /// Fetches a list of infractions filtered by the type provided.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <param name="type">The type of <see cref="InfractionType"/> to filter by.</param>
        /// <returns></returns>
        public async Task<Infraction> FetchInfractionForUserAsync(ulong subjectId, ulong moderatorId, InfractionType type)
        {
            await _authorizationService.RequireClaims(moderatorId, ClaimMapType.InfractionView);
            return await _infractionRepository.FetchInfractionForUserByTypeAsync(subjectId, type);
        }

        /// <summary>
        /// Fetches a list of infractions for a user.
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
        /// Fetches a list of all timed infractions.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            return await _infractionRepository.FetchTimedInfractionsAsync();
        }

        /// <summary>
        /// Updates the reason for the given infraction.
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
        /// Removes the infraction given.
        /// </summary>
        /// <param name="caseId">The ID of the infraction to remove.</param>
        /// <param name="reason">The reason for removing the infraction.</param>
        /// <param name="moderator"></param>
        /// <param name="saveChanges"></param>
        /// <returns></returns>
        public async Task RemoveInfractionAsync(string caseId, string reason, ulong moderator)
        {
            await _authorizationService.RequireClaims(moderator, ClaimMapType.InfractionDelete);
            var infraction = await _infractionRepository.FetchInfractionByIDAsync(caseId);

            if(infraction is null)
            {
                throw new InvalidOperationException($"The caseID provided does not exist.");
            }

            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var user = guild.GetUser(infraction.SubjectId);
            if (user is null)
            {
                // If the user is not in the guild, and it's a mute, AND it's already expired, we can safely remove it.
                if (infraction.Type == InfractionType.Mute)
                {
                    await _infractionRepository.DeleteAsync(infraction);
                }
                return;
            }
            var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var Type = infraction.Type;
            if (Type == InfractionType.Mute || Type == InfractionType.Ban)
            {
                switch (Type)
                {
                    case InfractionType.Mute:

                        await user.RemoveRoleAsync(muteRole);
                        break;

                    case InfractionType.Ban:

                        await guild.RemoveBanAsync(infraction.SubjectId);
                        break;

                }
            }
            await _infractionRepository.DeleteAsync(infraction);
            if (Type == InfractionType.Warn)
            {
                await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId, infraction.Type.ToString(), _client, caseId);
            }
            else
            {
                await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId, infraction.Type.ToString(), _client);
            }
        }

        /// <summary>
        /// Checks for multiple infractions, and if they have a multiple of 3, the user will be muted.
        /// </summary>
        /// <param name="userId">The user to query for.</param>
        /// <param name="guildId">The guild ID to check for.</param>
        /// <returns></returns>
        public async Task CheckForMultipleInfractionsAsync(ulong userId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            var infractions = await _infractionRepository.FetchAllUserInfractionsAsync(userId);
            if (infractions.Count() % 3 == 0)
            {
                await CreateInfractionAsync(user.Id, _client.CurrentUser.Id, guildId, InfractionType.Mute, "User incurred a number of infractions that was a multiple of 3.", TimeSpan.FromHours(6));
                var muteLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await muteLog.SendInfractionLogMessageAsync("User incurred a number of infractions that was a multiple of 3.", _client.CurrentUser.Id, user.Id, "Mute", _client, "6 hours");
            }
        }
    }
}
