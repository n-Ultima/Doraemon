using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Services;
using Doraemon.Data.Models;
using Doraemon.Common.Attributes;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Data;
using Doraemon.Common.Extensions;

namespace Doraemon.Modules
{
    [Name("Infractions")]
    [Group("infraction")]
    [Alias("infractions")]
    [Summary("Provides utilities for searching and managing infractions.")]
    public class InfractionModule : ModuleBase<SocketCommandContext>
    {
        public DoraemonContext _doraemonContext;
        public InfractionService _infractionService;
        public InfractionModule(DoraemonContext doraemonContext, InfractionService infractionService)
        {
            _doraemonContext = doraemonContext;
            _infractionService = infractionService;
        }
        [Command]
        [Alias("search")]
        [RequireInfractionAuthorization]
        [Priority(15)]
        [Summary("Lists all the infractions of a user.")]
        public async Task ListUserInfractionsAsync(
            [Summary("The user whose infractions to be displayed.")]
                SocketGuildUser user)
        {
            if ((Context.Channel as IGuildChannel).IsPublic())
            {
                return;
            }
            var infractions = await _infractionService.FetchUserInfractionsAsync(user.Id);
            var embed = new EmbedBuilder()
                .WithTitle($"Infractions for {user.GetFullUsername()}")
                .WithDescription(infractions.ToString())
                .Build();
            await ReplyAsync(embed: embed);
        }
        [Command("delete")]
        [Alias("remove")]
        [RequireInfractionAuthorization]
        [Summary("Deletes an infraction, causing it to no longer show up in future queries.")]
        public async Task DeleteInfractionAsync(
            [Summary("The ID of the infraction")]
                string infractionId)
        {
            await _infractionService.RemoveInfractionAsync(infractionId);
            await Context.AddConfirmationAsync();
        }
        [Command("update")]
        [Summary("Updates a current infraction with the given reason.")]
        [RequireInfractionAuthorization]
        public async Task UpdateInfractionAsync(
            [Summary("The ID of the infraction to update.")]
                string infractionId,
            [Summary("The new reason for the infraction")]
                [Remainder] string reason)
        {
            await _infractionService.UpdateInfractionAsync(infractionId, reason);
            await Context.AddConfirmationAsync();
        }
    }
}
