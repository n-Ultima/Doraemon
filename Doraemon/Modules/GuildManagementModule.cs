using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Events;

namespace Doraemon.Modules
{
    [Name("GuildManagement")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Provides utilies for managing the current guild.")]
    public class GuildManagementModule : ModuleBase
    {
        private readonly GuildEvents _guildEvents;
        private readonly GuildManagementService _guildManagementService;

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
            await _guildEvents.AutoConfigureMuteRoleAsync(Context.Guild);
            await Context.AddConfirmationAsync();
        }

        [Command("set punishment escalation")]
        [Summary("Sets the escalation punishment for the provided number of infractions.")]
        public async Task SetPunishmentEscalationAsync(
            [Summary("The number of infractions that is required for the trigger to occur.")]
                int numOfInfractions, 
            [Summary("The type of infraction.")]
                InfractionType type,
            [Summary("The duration of the punishment.")]
                TimeSpan? duration = null)
        {
            await _guildManagementService.AddPunishmentConfigurationAsync(Context.User.Id, numOfInfractions, type, duration);
            await Context.AddConfirmationAsync();
        }

        [Command("update punishment escalation")]
        [Summary("Edits an already existing escalation's type.")]
        public async Task ModifyPunishmentEscalationConfigAsync(
            [Summary("The number of punishments for the existing config to be fired.")]
                int num, 
            [Summary("The new infraction type.")]
                InfractionType type)
        {
            await _guildManagementService.ModifyPunishmentConfigurationAsync(num, type, null);
            await Context.AddConfirmationAsync();
        }
        [Command("update punishment escalation")]
        [Summary("Edits an already existing escalation's duration.")]
        public async Task ModifyPunishmentEscalationConfigAsync(
            [Summary("The number of punishments for the existing config to be fired.")]
                int num, 
            [Summary("The new duration of the duration.")]
                TimeSpan duration)
        {
            await _guildManagementService.ModifyPunishmentConfigurationAsync(num, null, duration);
            await Context.AddConfirmationAsync();
        }

        [Command("delete punishment escalation")]
        [Summary("Deletes an existing punishment configuration.")]
        public async Task DeletePunishmentConfigurationAsync(
            [Summary("The number of punishments needed to trigger the escalation.")]
            int numOfPunishments)
        {
            await _guildManagementService.DeletePunishmentConfigurationAsync(numOfPunishments);
            await Context.AddConfirmationAsync();
        }
    }
}