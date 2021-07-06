using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.Extensions;
using Doraemon.Services.Core;
using Doraemon.Services.Events;

namespace Doraemon.Modules
{
    [Name("GuildManagement")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Provides utilies for managing the current guild.")]
    public class GuildManagementModule : ModuleBase
    {
        public GuildEvents _guildEvents;
        public GuildManagementService _guildManagementService;

        public GuildManagementModule(GuildManagementService guildManagementService, GuildEvents guildEvents)
        {
            _guildManagementService = guildManagementService;
            _guildEvents = guildEvents;
        }

        [Command("raidmode enable")]
        [Summary("Enables raid mode on the server, preventing users from joining.")]
        public async Task EnableRaidModeAsync(
            [Summary("The reason for enabling raidmode.")] [Remainder]
            string reason = null)
        {
            await _guildManagementService.EnableRaidModeAsync(Context.User.Id, Context.Guild.Id,
                reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }

        [Command("raidmode disable")]
        [Summary("Disables raid mode, allowing user joins to occur.")]
        public async Task DisableRaidModeAsync(
            [Summary("Optional reason for disabling raid mode.")] [Remainder]
            string reason = null)
        {
            await _guildManagementService.DisableRaidModeAsync(Context.User.Id, Context.Guild.Id,
                reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }

        [Command("raidmode")]
        [Priority(10)]
        [Summary("Returns if raid mode is enabled or disabled.")]
        public async Task RaidModeStatusAsync()
        {
            var check = _guildManagementService.FetchCurrentRaidModeAsync();
            await ReplyAsync($"Raid mode is currently `{check}`");
        }

        [Command("setup muterole")]
        [Summary("Sets up the muterole, incase initial setup fails.")]
        public async Task SetupMuteRoleAsync()
        {
            await _guildEvents.SetupMuteRoleAsync(Context.Guild.Id);
            await Context.AddConfirmationAsync();
        }
    }
}