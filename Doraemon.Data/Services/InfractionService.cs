using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Core;
using Discord.WebSocket;
using Doraemon.Common.Utilities;
using Discord;
using Doraemon.Common;
using Discord.Net;
using Discord.Commands;

namespace Doraemon.Data.Services
{
    public class InfractionService
    {
        public DoraemonContext _doraemonContext;
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
            _doraemonContext.Infractions.Add(new Infraction { Id = await DatabaseUtilities.ProduceIdAsync(), ModeratorId = moderatorId, Reason = reason, SubjectId = subjectId, Type = type, CreatedAt = DateTime.Now, Duration = duration ?? null });
            var currentInfractions = await _doraemonContext.Infractions
                .AsQueryable()
                .Where(x => x.SubjectId == subjectId)
                .ToListAsync();
            await _doraemonContext.SaveChangesAsync();
            if (currentInfractions.Count % 3 == 0)
            {
                await CheckForMultipleInfractionsAsync(subjectId, guildId);
            }
        }
        public async Task<List<Infraction>> FetchUserInfractionsAsync(ulong subjectId)
        {
            var infractions = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.SubjectId == subjectId)
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
        public async Task RemoveInfractionAsync(string caseId)
        {
            var infraction = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.Id == caseId)
                .SingleOrDefaultAsync();
            if (infraction is null)
            {
                throw new ArgumentException("The caseId provided does not exist.");
            }
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var user = guild.GetUser(infraction.SubjectId);
            var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var Type = infraction.Type;
            switch (Type)
            {
                case InfractionType.Mute:
                    {
                        await user.RemoveRoleAsync(muteRole);
                        break;
                    }
                case InfractionType.Ban:
                    {
                        await guild.RemoveBanAsync(infraction.SubjectId);
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException("There was an error removing the infraction.");
                    }
            }
            _doraemonContext.Infractions.Remove(infraction);
            await _doraemonContext.SaveChangesAsync();
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
                await muteLog.SendInfractionLogMessageAsync("User incurred a number of infractions that was a multiple of 3.", _client.CurrentUser.Id, user.Id, "Mute");
            }
        }
    }
}
