using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Humanizer;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Debug")]
    [Group("debug")]
    [Description("Used to debug multipe things involving Doraemon.")]
    [RequireAuthorGuildPermissions(Permission.Administrator)]
    public class DebugModule : DoraemonGuildModuleBase
    {
        private static readonly LocalEmoji Warning = new("⚠️");
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        [Command("throw")]
        [Description("Throws an error")]
        public async Task ThrowAsync(
            [Description("The error to throw.")] [Remainder]
                string error = null)
        {
            await Context.Message.AddReactionAsync(Warning);
            if (error is null) error = "Exception generated due to a value not being provided.";
            throw new Exception(error);
        }

        [Command("guilds")]
        [Description("Lists all guilds that the current instance of Doraemon is currently in.")]
        public DiscordCommandResult ListGuilds()
        {
            var guilds = Bot.GetGuilds().Select(x => x.Value.Name);
            var guildNames = guilds.Humanize();
            return Response($"This instance of Doraemon is currently joined to {guilds.Count()} guilds.\n```\n{string.Join("\n", guildNames)}\n```");
        }

        [Command("leave")]
        [Description("Leaves the guild provided.")]
        public async Task<DiscordCommandResult> LeaveGuildAsync(Snowflake guildId)
        {
            var guild = Bot.GetGuild(guildId);
            if (guild is null)
                throw new ArgumentException("Doraemon is not currently joined to a guild with that ID.");
            if (guild.Id == DoraemonConfig.MainGuildId)
            {
                throw new InvalidOperationException($"Leaving the main guild provided in config.json can provide multiple issues.");
            }
            await guild.LeaveAsync(new DefaultRestRequestOptions
            {
                Reason = "A leave was requested by the bot's administrator."
            });
            return Confirmation();
        }
    }
}