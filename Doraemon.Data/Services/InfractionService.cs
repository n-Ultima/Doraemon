﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Core;
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
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public InfractionService(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        public async Task CreateInfractionAsync(ulong subjectId, ulong moderatorId, ulong guildId, InfractionType type, string reason, TimeSpan? duration)
        {
            _doraemonContext.Infractions.Add(new Infraction { Id = await DatabaseUtilities.ProduceIdAsync(), ModeratorId = moderatorId, Reason = reason, SubjectId = subjectId, Type = type, CreatedAt = DateTimeOffset.Now, Duration = duration ?? null });
            var currentInfractions = await _doraemonContext.Infractions
                .AsQueryable()
                .Where(x => x.SubjectId == subjectId)
                .Where(x => x.ModeratorId != x.SubjectId) // Gets rid of selfmutes
                .Where(x => x.Type != InfractionType.Note) // Don't get notes
                .ToListAsync();
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(subjectId);
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            if(type == InfractionType.Ban || type == InfractionType.Mute)
            {
                switch (type)
                {
                    case (InfractionType.Ban):
                        await guild.AddBanAsync(user, 0, reason);
                        break;
                    case (InfractionType.Mute):
                        await user.AddRoleAsync(mutedRole);
                        break;
                }
            }
            await _doraemonContext.SaveChangesAsync();
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            if(duration is null)
            {
                await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), "None");
            }
            else
            {
                await modLog.SendInfractionLogMessageAsync(reason, moderatorId, subjectId, type.ToString(), duration.Value.Humanize());
            }
            if (currentInfractions.Count % 3 == 0)
            {
                await CheckForMultipleInfractionsAsync(subjectId, guildId);
            }
        }
        public async Task<IEnumerable<Infraction>> FetchUserInfractionsAsync(ulong subjectId)
        {
            var infractions = await _doraemonContext.Infractions
                .Where(x => x.SubjectId == subjectId)
                .ToListAsync();
            return infractions;
        }
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            var infractions = await _doraemonContext.Infractions
                .Where(x => x.Duration != null)
                .ToListAsync();
            return infractions;
        }
        public async Task UpdateInfractionAsync(string caseId, string newReason)
        {
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
        public async Task RemoveInfractionAsync(string caseId, string reason, ulong moderator, bool saveChanges)
        {
            var infraction = await _doraemonContext.Infractions
                .Where(x => x.Id == caseId)
                .SingleOrDefaultAsync();
            if (infraction is null)
            {
                throw new ArgumentException("The caseId provided does not exist.");
            }
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var user = guild.GetUser(infraction.SubjectId);
            var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
            var Type = infraction.Type;
            if(Type == InfractionType.Mute || Type == InfractionType.Ban)
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
