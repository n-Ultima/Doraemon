using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("GuildManagement")]
    [Description("Provides utilies for managing the current guild.")]
    public class GuildManagementModule : DiscordGuildModuleBase
    {
        private readonly GuildManagementService _guildManagementService;

        public GuildManagementModule(GuildManagementService guildManagementService)
        {
            _guildManagementService = guildManagementService;
        }

        [Command("raidmode-enable")]
        [Description("Enables raid mode on the server, preventing users from joining.")]
        public async Task EnableRaidModeAsync(
            [Description("The reason for enabling raidmode.")] [Remainder]
                string reason = null)
        {
            await _guildManagementService.EnableRaidModeAsync(Context.Author.Id, Context.Guild.Id,
                reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }

        [Command("raidmode-disable")]
        [Description("Disables raid mode, allowing user joins to occur.")]
        public async Task DisableRaidModeAsync(
            [Description("Optional reason for disabling raid mode.")] [Remainder]
                string reason = null)
        {
            await _guildManagementService.DisableRaidModeAsync(Context.Author.Id, Context.Guild.Id,
                reason ?? "Not specified");
            await Context.AddConfirmationAsync();
        }

        [Command("raidmode")]
        [Priority(10)]
        [Description("Returns if raid mode is enabled or disabled.")]
        public async Task RaidModeStatusAsync()
        {
            var check = _guildManagementService.FetchCurrentRaidModeAsync();
            await Context.Channel.SendMessageAsync(new LocalMessage().WithContent($"Raid mode is currently `{check}`"));
        }

        [Command("set-punishment-escalation")]
        [Description("Sets the escalation punishment for the provided number of infractions.")]
        public async Task SetPunishmentEscalationAsync(
            [Description("The number of infractions that is required for the trigger to occur.")]
                int numOfInfractions, 
            [Description("The type of infraction.")]
                InfractionType type,
            [Description("The duration of the punishment.")]
                TimeSpan? duration = null)
        {
            await _guildManagementService.AddPunishmentConfigurationAsync(numOfInfractions, type, duration);
            await Context.AddConfirmationAsync();
        }

        [Command("update-punishment-escalation")]
        [Description("Edits an already existing escalation's type.")]
        public async Task ModifyPunishmentEscalationConfigAsync(
            [Description("The number of punishments for the existing config to be fired.")]
                int num, 
            [Description("The new infraction type.")]
                InfractionType type)
        {
            await _guildManagementService.ModifyPunishmentConfigurationAsync(num, type, null);
            await Context.AddConfirmationAsync();
        }
        [Command("update-punishment-escalation")]
        [Description("Edits an already existing escalation's duration.")]
        public async Task ModifyPunishmentEscalationConfigAsync(
            [Description("The number of punishments for the existing config to be fired.")]
                int num, 
            [Description("The new duration of the duration.")]
                TimeSpan duration)
        {
            await _guildManagementService.ModifyPunishmentConfigurationAsync(num, null, duration);
            await Context.AddConfirmationAsync();
        }

        [Command("delete-punishment-escalation")]
        [Description("Deletes an existing punishment configuration.")]
        public async Task DeletePunishmentConfigurationAsync(
            [Description("The number of punishments needed to trigger the escalation.")]
                int numOfPunishments)
        {
            await _guildManagementService.DeletePunishmentConfigurationAsync(numOfPunishments);
            await Context.AddConfirmationAsync();
        }
    }
}