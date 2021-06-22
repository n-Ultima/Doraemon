using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common.CommandHelp;
using Discord;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Common.Utilities;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Humanizer;

namespace Doraemon.Modules
{
    [Name("Debug")]
    [Summary("Used to debug multipe things involving Doraemon.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [HiddenFromHelp]
    public class DebugModule : ModuleBase
    {
        public DiscordSocketClient _client;
        public DoraemonContext _doraemonContext;
        public DebugModule(DiscordSocketClient client, DoraemonContext doraemonContext)
        {
            _client = client;
            _doraemonContext = doraemonContext;
        }
        [Command("throw")]
        [Summary("Throws an error")]
        public async Task ThrowAsync(
            [Summary("The error to throw.")]
                [Remainder] string error = null)
        {
            if(error is null)
            {
                error = "Exception generated due to a value not being provided.";
            }
            throw new Exception(error);
        }
        [Command("guilds")]
        [Summary("Lists all guilds that the current instance of Doraemon is currently in.")]
        public async Task ListAllGuildsAsync()
        {
            var guilds = _client.Guilds.Count;
            await ReplyAsync($"This instance of Doraemon is currently joined to {guilds} guilds.");
        }
        [Command("leave")]
        [Summary("Leaves the guild provided.")]
        public async Task LeaveGuildAsync(ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            if(guild is null)
            {
                throw new ArgumentNullException("Doraemon is not currently joined to a guild with that ID.");
            }
            await guild.LeaveAsync(new RequestOptions()
            {
                AuditLogReason = "A leave was requested by the bot's administrator."
            });
        }
    }
}
