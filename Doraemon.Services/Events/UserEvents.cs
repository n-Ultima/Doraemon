using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Humanizer;

namespace Doraemon.Services.Events
{
    public class UserEvents
    {
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DiscordSocketClient _client;
        private readonly DoraemonContext _doraemonContext;
        public GuildManagementService _guildManagementService;

        public UserEvents(DoraemonContext doraemonContext, DiscordSocketClient client,
            GuildManagementService guildManagementService)
        {
            _doraemonContext = doraemonContext;
            _client = client;
            _guildManagementService = guildManagementService;
        }

        public static DoraemonConfiguration DoraemonConfig { get; } = new();

        public async Task UserJoined(SocketGuildUser user) // Fired when a new user joins the guild.
        {
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            if (_guildManagementService.RaidModeEnabled)
            {
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                try
                {
                    await dmChannel.SendMessageAsync(
                        $"You were kicked from {guild.Name} for reason: Automatic kick due to raid mode.");
                }
                catch (HttpException ex) when (ex.DiscordCode == 50007)
                {
                    Console.WriteLine(
                        $"{user.GetFullUsername()} was kicked due to raid mode, I was unable to DM them.");
                }

                await user.KickAsync("Raid mode");
                var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                await modLog.SendInfractionLogMessageAsync("Automatic kick due to raid mode.", _client.CurrentUser.Id,
                    user.Id, "Kick", _client);
            }

            var autoModId = _client.CurrentUser.Id;
            // Checks for mute evades.
            var checkForTemp = await _doraemonContext.Infractions
                .AsAsyncEnumerable()
                .Where(x => x.SubjectId == user.Id)
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.CreatedAt + x.Duration >= DateTimeOffset.Now)
                .FirstOrDefaultAsync();
            if (checkForTemp is not null)
            {
                var modLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.ModLogChannelId);
                var role = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
                await user.AddRoleAsync(role);
                await modLog.SendInfractionLogMessageAsync(
                    $"Reapplied active mute for {user.GetFullUsername()} upon rejoin.", _client.CurrentUser.Id, user.Id,
                    "Mute", _client, $"{(checkForTemp.CreatedAt - DateTimeOffset.Now).Humanize()}");
            }

            // Logging for new users
            if (DoraemonConfig.LogConfiguration.UserJoinedLogChannelId == default) return;
            var newUserLog = guild.GetTextChannel(DoraemonConfig.LogConfiguration.UserJoinedLogChannelId);
            var userEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("User Joined Log")
                .AddField("User: ", user)
                .AddField("UserId: ", user.Id)
                .AddField("Account Creation: ", user.CreatedAt.ToString("f"))
                .AddField("Joined Server: ", user.JoinedAt.Value.ToString("f")); // Embed for logging new users.
            await newUserLog.SendMessageAsync(embed: userEmbed.Build());
        }
    }
}