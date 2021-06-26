using System;
using System.Collections.Generic;
using System.Linq;
using Doraemon.Data.Models.Core;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Moderation;
using Discord.WebSocket;
using Humanizer;
using Doraemon.Common.Utilities;
using Discord;
using Doraemon.Common;
using Discord.Net;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Services
{
    public class InfractionService
    {
        public DoraemonContext _doraemonContext;
        public IServiceScopeFactory _serviceScopeFactory;
        public DiscordSocketClient _client;
        public AuthorizationService _authorizationService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public InfractionService(DoraemonContext doraemonContext, DiscordSocketClient client, AuthorizationService authorizationService)
        {
            _doraemonContext = doraemonContext;
            _client = client;
            _authorizationService = authorizationService;
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
            _doraemonContext.Infractions.Add(new Infraction { Id = await DatabaseUtilities.ProduceIdAsync(), ModeratorId = moderatorId, Reason = reason, SubjectId = subjectId, Type = type, CreatedAt = DateTimeOffset.Now, Duration = duration ?? null });
            var currentInfractions = await _doraemonContext.Infractions
                .AsQueryable()
                .Where(x => x.SubjectId == subjectId)
                .Where(x => x.ModeratorId != x.SubjectId) // Gets rid of selfmutes
                .Where(x => x.Type != InfractionType.Note) // Don't get notes
                .ToListAsync();
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(subjectId);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            if (type == InfractionType.Ban || type == InfractionType.Mute)
            {
                switch (type)
                {
                    case (InfractionType.Ban):
                        try
                        {
                            await dmChannel.SendMessageAsync($"You have been banned from {guild.Name}. Reason: {reason}");
                            await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString());

                        }
                        catch (HttpException)
                        {
                            await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                        }
                        await guild.AddBanAsync(user, 0, reason);
                        break;
                    case (InfractionType.Mute):
                        await user.AddRoleAsync(mutedRole);
                        try
                        {
                            await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), duration.Value.Humanize());
                            await dmChannel.SendMessageAsync($"You have been muted in {guild.Name}. Reason: {reason}\nDuration: {duration.Value.Humanize()}");
                        }
                        catch (HttpException)
                        {
                            await modLog.SendMessageAsync($"I was unable to DM the user for the above infraction.");
                        }
                        break;
                }
            }
            await _doraemonContext.SaveChangesAsync();
            if (currentInfractions.Count % 3 == 0)
            {
                await CheckForMultipleInfractionsAsync(subjectId, guildId);
            }
            if (type == InfractionType.Warn)
            {
                await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString());
                try
                {
                    await dmChannel.SendMessageAsync($"You have received a warning in {guild.Name}. Reason: {reason}");
                }
                catch (HttpException)
                {
                    await modLog.SendMessageAsync("I was unable to DM the user for the above infraction.");
                }
            }
            else if (type == InfractionType.Note)
            {
                await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString());
            }
        }

        /// <summary>
        /// Fetches a list of infractions filtered by the type provided.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <param name="type">The type of <see cref="InfractionType"/> to filter by.</param>
        /// <returns></returns>
        public async Task<Infraction> FetchInfractionForUserAsync(ulong subjectId, InfractionType type)
        {
            return await _doraemonContext.Infractions
                .Where(x => x.SubjectId == subjectId)
                .Where(x => x.Type == type)
                .SingleOrDefaultAsync();
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
            var infractions = await _doraemonContext.Infractions
                .Where(x => x.SubjectId == subjectId)
                .ToListAsync();
            return infractions;
        }

        /// <summary>
        /// Fetches a list of all timed infractions.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            var infractions = await _doraemonContext.Infractions
                .Where(x => x.Duration != null)
                .ToListAsync();
            return infractions;
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
            var infraction = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.Id == caseId)
                .SingleOrDefaultAsync();
            if (infraction is null)
            {
                throw new ArgumentException("The caseId provided does not exist.");
            }
            infraction.Reason = newReason;
            await _doraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Removes the infraction given.
        /// </summary>
        /// <param name="caseId">The ID of the infraction to remove.</param>
        /// <param name="reason">The reason for removing the infraction.</param>
        /// <param name="moderator"></param>
        /// <param name="saveChanges"></param>
        /// <returns></returns>
        public async Task RemoveInfractionAsync(string caseId, string reason, ulong moderator, bool saveChanges)
        {
            await _authorizationService.RequireClaims(moderator, ClaimMapType.InfractionDelete);
            var infraction = await _doraemonContext.Infractions
                .Where(x => x.Id == caseId)
                .SingleOrDefaultAsync();
            if (infraction is null)
            {
                throw new ArgumentException("The caseId provided does not exist.");
            }
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var user = guild.GetUser(infraction.SubjectId);
            if (user is null)
            {
                // If the user is not in the guild, and it's a mute, AND it's already expired, we can safely remove it.
                if (infraction.Type == InfractionType.Mute)
                {
                    _doraemonContext.Infractions.Remove(infraction);
                    if (saveChanges)
                    {
                        await _doraemonContext.SaveChangesAsync();
                    }
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
            _doraemonContext.Infractions.Remove(infraction);
            if (saveChanges)
            {
                await _doraemonContext.SaveChangesAsync();
            }
            await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId, infraction.Type.ToString());
        }
        public async Task CheckForMultipleInfractionsAsync(ulong userId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            var infractions = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.SubjectId == userId)
                .Where(x => x.Type != InfractionType.Note)
                .ToListAsync();
            if (infractions.Count % 3 == 0)
            {
                await CreateInfractionAsync(user.Id, _client.CurrentUser.Id, guildId, InfractionType.Mute, "User incurred a number of infractions that was a multiple of 3.", TimeSpan.FromHours(6));
                var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
                await user.AddRoleAsync(muteRole);
                var muteLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await muteLog.SendInfractionLogMessageAsync("User incurred a number of infractions that was a multiple of 3.", _client.CurrentUser.Id, user.Id, "Mute", "6 hours");
            }
        }
    }
}
