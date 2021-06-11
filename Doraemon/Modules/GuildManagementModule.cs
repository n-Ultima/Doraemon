using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.Extensions;
using Doraemon.Data.Services;

namespace Doraemon.Modules
{
    [Name("GuildManagement")]
    [Group("raidmode")]
    [Summary("Provides utilies for managing the current guild.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class GuildManagementModule : ModuleBase
    {
        public GuildManagementService _guildManagementService;
        public GuildManagementModule(GuildManagementService guildManagementService)
        {
            _guildManagementService = guildManagementService;
        }
        
        [Command("enable")]
        [Summary("Enables raid mode on the server, preventing users from joining.")]
        public async Task EnableRaidModeAsync(
            [Summary("The reason for enabling raidmode.")]
                [Remainder] string reason = null)
        {
            await _guildManagementService.EnableRaidModeAsync(Context.User.Id, Context.Guild.Id, reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }
        [Command("disable")]
        [Summary("Disables raid mode, allowing user joins to occur.")]
        public async Task DisableRaidModeAsync(
            [Summary("Optional reason for disabling raid mode.")]
                [Remainder] string reason = null)
        {
            await _guildManagementService.DisableRaidModeAsync(Context.User.Id, Context.Guild.Id, reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }
        [Command]
        [Summary("Returns if raid mode is enabled or disabled.")]
        public async Task RaidModeStatusAsync()
        {
            var check = await _guildManagementService.FetchCurrentRaidModeAsync();
            await ReplyAsync($"Raid mode is currently `{check}`");
        }
    }
}
