using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Common;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Events
{
    public class UserEvents
    {
        public static DoraemonConfiguration Configuration { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public UserEvents(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        public async Task UserJoined(SocketGuildUser user)// Fired when a new user joins the guild.
        {
            ulong autoModId = _client.CurrentUser.Id;
            var guild = _client.GetGuild(Configuration.MainGuildId);
            // Checks for mute evades.
            var checkForMute = await _doraemonContext
                .Set<Infraction>()
                .AsQueryable()
                .Where(x => x.SubjectId == user.Id)
                .Where(x => x.Type == InfractionType.Mute)
                .FirstOrDefaultAsync();
            if (checkForMute is not null)
            {
                var role = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
                await user.AddRoleAsync(role);
            }
            var checkForTemp = await _doraemonContext
                .Set<Infraction>()
                .AsQueryable()
                .Where(x => x.SubjectId == user.Id)
                .Where(x => x.Type == InfractionType.Mute)
                .FirstOrDefaultAsync();
            if (checkForTemp is not null)
            {
                var role = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
                await user.AddRoleAsync(role);
                CommandHandler.Mutes.Add(new Mute { Guild = guild, End = DateTime.Now + TimeSpan.FromDays(1), Role = role, User = user });
            }
            // Logging for new users
            if (Configuration.LogConfiguration.UserJoinedLogChannelId == default)
            {
                return;
            }
            var newUserLog = guild.GetTextChannel(Configuration.LogConfiguration.UserJoinedLogChannelId);
            var userEmbed = new EmbedBuilder()
                .WithColor(Discord.Color.Green)
                .WithTitle("User Joined Log")
                .AddField("User: ", user)
                .AddField("UserId: ", user.Id)
                .AddField("Account Creation: ", user.CreatedAt.ToString("f"))
                .AddField("Joined Server: ", user.JoinedAt.Value.ToString("f"));// Embed for logging new users.
            await newUserLog.SendMessageAsync(embed: userEmbed.Build());
        }
    }
}
