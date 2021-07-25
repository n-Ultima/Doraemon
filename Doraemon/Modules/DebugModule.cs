using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.CommandHelp;
using Doraemon.Data;
using Doraemon.Services.Core;
using Humanizer;

namespace Doraemon.Modules
{
    [Name("Debug")]
    [Group("debug")]
    [Summary("Used to debug multipe things involving Doraemon.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [HiddenFromHelp]
    public class DebugModule : ModuleBase
    {
        private static readonly Emoji Warning = new("⚠️");
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private DiscordSocketClient _client;

        public DebugModule(DiscordSocketClient client, DoraemonContext doraemonContext)
        {
            _client = client;
        }

        [Command("throw")]
        [Summary("Throws an error")]
        public async Task ThrowAsync(
            [Summary("The error to throw.")] [Remainder]
            string error = null)
        {
            await Context.Message.AddReactionAsync(Warning);
            if (error is null) error = "Exception generated due to a value not being provided.";
            throw new Exception(error);
        }

        [Command("guilds")]
        [Summary("Lists all guilds that the current instance of Doraemon is currently in.")]
        public async Task ListAllGuildsAsync()
        {
            var guilds = _client.Guilds;
            var guildNames = guilds.Humanize();
            await ReplyAsync($"This instance of Doraemon is currently joined to {guilds.Count()} guilds.\n```\n{string.Join("\n", guildNames)}\n```");
        }

        [Command("leave")]
        [Summary("Leaves the guild provided.")]
        public async Task LeaveGuildAsync(ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            if (guild is null)
                throw new ArgumentNullException("Doraemon is not currently joined to a guild with that ID.");
            if (guild.Id == DoraemonConfig.MainGuildId)
            {
                throw new InvalidOperationException($"Leaving the main guild provided in config.json can provide multiple issues.");
            }
            await guild.LeaveAsync(new RequestOptions
            {
                AuditLogReason = "A leave was requested by the bot's administrator."
            });
        }
    }
}