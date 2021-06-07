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
        /// <summary>
        /// Creates an infraction.
        /// </summary>
        /// <param name="subjectId"></param>
        /// <param name="moderatorId"></param>
        /// <param name="type"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task CreateInfractionAsync(ulong subjectId, ulong moderatorId, ulong guildId, InfractionType type, string reason)
        {
            var currentInfractions = await _doraemonContext.Infractions
                .AsQueryable()
                .Where(x => x.SubjectId == subjectId)
                .ToListAsync();
            _doraemonContext.Infractions.Add(new Infraction { Id = await DatabaseUtilities.ProduceIdAsync(), ModeratorId = moderatorId, Reason = reason, SubjectId = subjectId, Type = type});
            await _doraemonContext.SaveChangesAsync();
            if (currentInfractions.Count % 3 == 0)
            {
                await CheckForMultipleInfractionsAsync(subjectId, guildId);
            }
        }
        public async Task<StringBuilder> FetchUserInfractionsAsync(ulong subjectId)
        {
            var infractions = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.SubjectId == subjectId)
                .ToListAsync();
            if (!infractions.Any())
            {
                var b = new StringBuilder()
                    .AppendLine("No infractions found.");
                return b;
            }
            var builder = new StringBuilder();
            foreach(var infraction in infractions)
            {
                builder.AppendLine($"Infraction Type: {infraction.Type}");
                builder.AppendLine($"Punishment ID: {infraction.Id}");
                builder.AppendLine($"Subject: <@{subjectId}>");
                builder.AppendLine($"Moderator: <@{infraction.ModeratorId}>");
                builder.AppendLine($"Reason: {infraction.Reason}");
            }
            return builder;
        }
        /// <summary>
        /// Updates the given infraction ID's reason.
        /// </summary>
        /// <param name="caseId"></param>
        /// <param name="newReason"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Removes an infraction from the infractions table.
        /// </summary>
        /// <param name="caseId"></param>
        /// <returns></returns>
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
            _doraemonContext.Infractions.Remove(infraction);
            await _doraemonContext.SaveChangesAsync();
        }
        public async Task CheckForMultipleInfractionsAsync(ulong userId, ulong guildId)
        {
            var user = _client.GetUser(userId);
            var guild = _client.GetGuild(guildId);
            var infractions = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.SubjectId == userId)
                .Where(x => x.Type != InfractionType.Note)
                .ToListAsync();
            if (infractions.Count % 3 == 0)
            {
                await CreateInfractionAsync(user.Id, _client.CurrentUser.Id, guildId, InfractionType.Mute, "User incurred a number of infractions that was a multiple of 3.");
                var embed = new EmbedBuilder()
                    .WithTitle("You were muted")
                    .WithColor(Color.DarkRed)
                    .WithDescription($"You were muted in {guild.Name}\n\nYou were muted for reason: User incurred a number of infractions that was a multiple of 3.\nMute Expiration: 6 hours.")
                    .Build();
                try
                {
                    await user.SendMessageAsync(embed: embed);
                }
                catch (HttpException ex) when (ex.DiscordCode == 50007)
                {
                    Console.WriteLine("Unable to DM user.");
                }
                var muteRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
                CommandHandler.Mutes.Add(new Models.Mute { End = DateTime.Now + TimeSpan.FromHours(6), Guild = guild, Role = muteRole, User = user as SocketGuildUser });
                await (user as SocketGuildUser).AddRoleAsync(muteRole);
                var muteLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await muteLog.SendInfractionLogMessageAsync("Sending messages that contain prohibited words", _client.CurrentUser.Id, user.Id, "Mute");
            }
        }
    }
}
