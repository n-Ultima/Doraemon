using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Http;
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
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Services.Moderation
{
    [DoraemonService]
    public class InfractionService : DoraemonBotService
    {
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        private readonly AuthorizationService _authorizationService;
        private readonly GuildManagementService _guildManagementService;
        public ModerationConfiguration ModerationConfig { get; private set; } = new();

        public InfractionService(IServiceProvider serviceProvider, AuthorizationService authorizationService,
            GuildManagementService guildManagementService) : base(serviceProvider)
        {
            _authorizationService = authorizationService;
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
                _authorizationService.RequireClaims(_createInfractionClaimsByType[type]);
            }

            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var currentInfractionsBeforeInfraction = await infractionRepository.FetchAllUserInfractionsAsync(subjectId);
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
                var moderatorUser = guild.GetMember(moderatorId);
                var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                switch (type)
                {
                    case InfractionType.Note:
                        var modLogChannel = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                        await modLogChannel.SendInfractionLogMessageAsync(reason, moderatorUser.Id, gUser.Id, "Note", Bot);
                        break;
                    case InfractionType.Ban:
                        var bannedGuildUser = await guild.FetchBanAsync(subjectId);
                        if (bannedGuildUser != null)
                        {
                            throw new Exception($"User already has an active ban infraction.");
                        }
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

                        await guild.CreateBanAsync(subjectId, reason, 0, new DefaultRestRequestOptions()
                        {
                            Reason = $"{moderatorUser.Tag}(ID: {moderatorUser.Id}: {reason}"
                        });
                        await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), Bot);
                        break;
                    case InfractionType.Mute:
                        if (gUser == null)
                            throw new InvalidOperationException($"The user provided is not currently in the guild, so I can't mute them.");
                        var mutedRole = guild.Roles.Where(x => x.Value.Name == muteRoleName).SingleOrDefault();

                        await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), Bot, duration.Value.Humanize(maxUnit: TimeUnit.Year, precision: 10));
                        await gUser.GrantRoleAsync(mutedRole.Value.Id);
                        try
                        {
                            await gUser.SendMessageAsync(new LocalMessage().WithContent($"You have been muted in {guild.Name}. Reason: {reason}\nDuration: {duration.Value.Humanize()}"));
                        }
                        catch (RestApiException)
                        {
                        }

                        break;
                    case InfractionType.Warn:
                        if (gUser == null)
                            throw new Exception($"The user is not currently in the guild, so I can't warn them.");
                        await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), Bot);
                        try
                        {
                            await gUser.SendMessageAsync(new LocalMessage()
                                .WithContent($"You have received a warning in {guild.Name}. Reason: {reason}"));
                        }
                        catch (RestApiException)
                        {
                        }

                        break;
                    case InfractionType.Kick:
                        if (gUser == null)
                            throw new Exception("The user is not currently in the guild, so I can't kick them.");
                        await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), Bot);
                        try
                        {
                            await gUser.SendMessageAsync(new LocalMessage()
                                .WithContent($"You have been kicked from {guild.Name}. Reason: {reason}"));
                        }
                        catch(RestApiException)
                        {}

                        await gUser.KickAsync(new DefaultRestRequestOptions
                        {
                            Reason = reason
                        });
                        break;
                }

                await infractionRepository.CreateAsync(new InfractionCreationData()
                {
                    Id = DatabaseUtilities.ProduceId(),
                    SubjectId = subjectId,
                    ModeratorId = moderatorId,
                    Duration = duration,
                    Type = type,
                    Reason = reason,
                    // We don't want an expiring infraction if it doesn't have value.
                    ExpiresAt = duration.HasValue
                        ? DateTimeOffset.UtcNow + duration
                        : null
                });


                if (!isEscalation)
                {
                    // If we try to escalate a mute or ban, it could lead to odd results. 
                    if (type == InfractionType.Warn)
                    {
                        await CheckForMultipleInfractionsAsync(subjectId, guild.Id);
                    }
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                return await infractionRepository.FetchAllUserInfractionsAsync(subjectId);
            }
        }

        public async Task<Infraction> FetchInfractionAsync(string infractionId)
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionView);
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                return await infractionRepository.FetchInfractionByIdAsync(infractionId);
            }
        }
        public async Task UpdateTimedInfractionDurationAsync(string infractionId, TimeSpan newDuration)
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionUpdate);
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var infraction = await infractionRepository.FetchInfractionByIdAsync(infractionId);
                if (infraction == null)
                    throw new Exception("The infraction ID provided doesn't exist.");
                if (infraction.Type == InfractionType.Note || infraction.Type == InfractionType.Warn)
                    throw new Exception($"An infraction with type {infraction.Type} can't have a duration.");
                if (infraction.Duration == newDuration)
                    throw new Exception($"You can't update an infraction's duration with a value that's equal to the original duration.");
                await infractionRepository.UpdateTimedInfractionAsync(infractionId, newDuration);
            }
        }
        /// <summary>
        ///     Fetches a list of all timed infractions.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            _authorizationService.RequireClaims(ClaimMapType.InfractionView);
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var results = await infractionRepository.FetchTimedInfractionsAsync();
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var infractionToUpdate = await infractionRepository.FetchInfractionByIdAsync(caseId);
                if (infractionToUpdate == null)
                    throw new Exception($"The infraction ID provided does not currently exist.");
                await infractionRepository.UpdateAsync(infractionToUpdate.Id, newReason);
                var modLogChannel = Bot.GetGuild(DoraemonConfig.MainGuildId).GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await modLogChannel.SendUpdatedInfractionLogMessageAsync(infractionToUpdate.Id, infractionToUpdate.Type.ToString(), _authorizationService.CurrentUser, newReason, Bot);
            }
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var infraction = await infractionRepository.FetchInfractionByIdAsync(caseId);

                if (infraction is null) throw new ArgumentException("The caseID provided does not exist.");

                var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
                var user = guild.GetMember(infraction.SubjectId);
                if (user is null)
                {
                    // If the user is not in the guild, and it's a mute, AND it's already expired, we can safely remove it.
                    if (infraction.Type == InfractionType.Mute)
                    {
                        Log.Logger.Information($"User {infraction.SubjectId} was not present in the server at the time of unmute.");
                        await infractionRepository.DeleteAsync(infraction);
                        return;
                    }

                    Log.Logger.Information($"User is null, attempting to remove infraction {infraction.Id}");
                }

                var muteRole = guild.Roles.FirstOrDefault(x => x.Value.Name == muteRoleName);
                var modLog = guild.GetChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                var type = infraction.Type;

                switch (type)
                {
                    case InfractionType.Mute:

                        await user.RevokeRoleAsync(muteRole.Value.Id);
                        await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId,
                            infraction.Type.ToString(), Bot);
                        break;

                    case InfractionType.Ban:
                        try
                        {
                            var banToDelete = await guild.FetchBanAsync(infraction.SubjectId);
                            if (banToDelete == null)
                            {
                                goto SkipTryCatch;
                            }

                            await guild.DeleteBanAsync(banToDelete.User.Id);
                        }
                        catch
                        {
                        }

                        SkipTryCatch:
                        await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId, infraction.Type.ToString(), Bot);
                        break;
                    case InfractionType.Note:
                        break;
                    case InfractionType.Warn:
                        await modLog.SendRescindedInfractionLogMessageAsync(reason, moderator, infraction.SubjectId,
                            infraction.Type.ToString(), Bot, caseId);
                        break;
                }

                await infractionRepository.DeleteAsync(infraction);
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
            var guild = Bot.GetGuild(guildId);
            var user = guild.GetMember(userId);
            if (user is null)
                return;
            using (var scope = ServiceProvider.CreateScope())
            {
                var infractionRepository = scope.ServiceProvider.GetRequiredService<InfractionRepository>();
                var infractions = await infractionRepository.FetchWarnsAsync(userId);
                switch (infractions.Count())
                {
                    case 1:
                        var oneWarn = await _guildManagementService.FetchPunishementConfigurationAsync(1);
                        if (oneWarn is null)
                            break;
                        await CreateInfractionAsync(userId, Bot.CurrentUser.Id, guild.Id, oneWarn.Type,
                            "Automatic punishment (strike 1)", true, oneWarn.Duration);
                        break;
                    case 2:
                        var twoWarns = await _guildManagementService.FetchPunishementConfigurationAsync(2);
                        if (twoWarns is null)
                            break;
                        await CreateInfractionAsync(userId, Bot.CurrentUser.Id, guildId, twoWarns.Type,
                            "Automatic punishment (strike 2)", true, twoWarns.Duration);
                        break;
                    case 3:
                        var threeWarns = await _guildManagementService.FetchPunishementConfigurationAsync(3);
                        if (threeWarns is null)
                            break;
                        await CreateInfractionAsync(userId, Bot.CurrentUser.Id, guildId, threeWarns.Type,
                            "Automatic punishment (strike 3)", true, threeWarns.Duration);
                        break;
                    case 4:
                        var fourWarns = await _guildManagementService.FetchPunishementConfigurationAsync(4);
                        if (fourWarns is null)
                            break;
                        await CreateInfractionAsync(userId, Bot.CurrentUser.Id, guildId, fourWarns.Type,
                            "Automatic punishment (strike 4)", true, fourWarns.Duration);
                        break;
                    case 5:
                        var fiveWarns = await _guildManagementService.FetchPunishementConfigurationAsync(5);
                        if (fiveWarns is null)
                            break;
                        await CreateInfractionAsync(userId, Bot.CurrentUser.Id, guildId, fiveWarns.Type,
                            "Automatic punishment (strike 5)", true, fiveWarns.Duration);
                        break;
                }
            }
        }
        private static readonly Dictionary<InfractionType, ClaimMapType> _createInfractionClaimsByType
            = new()
            {
                {InfractionType.Note, ClaimMapType.InfractionNote},
                {InfractionType.Warn, ClaimMapType.InfractionWarn},
                {InfractionType.Mute, ClaimMapType.InfractionMute},
                {InfractionType.Ban, ClaimMapType.InfractionBan},
                {InfractionType.Kick, ClaimMapType.InfractionKick},
            };
    }
}