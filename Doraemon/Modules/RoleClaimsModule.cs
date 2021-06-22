using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using Doraemon.Data.Services;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Doraemon.Common.CommandHelp;

namespace Doraemon.Modules
{
    [Name("RoleClaims")]
    [Group("role claims")]
    [Alias("role claim")]
    [RequireContext(ContextType.Guild)]
    [Summary("Provides helpful configuration for managing claims and roles.")]
    [HelpTags("claim", "role claim", "roles")]
    public class RoleClaimsModule : ModuleBase
    {
        public RoleClaimService _roleClaimService;
        public RoleClaimsModule(RoleClaimService roleClaimService)
        {
            _roleClaimService = roleClaimService;
        }

        [Command("add")]
        [Priority(10)]
        [Summary("Adds a claim to the given role.")]
        public async Task AddRoleClaimAsync(
            [Summary("The role for the claim to be granted.")]
                IRole role,
            [Summary("The claim to be added.")]
                ClaimMapType claimType)
        {
            await _roleClaimService.AddRoleClaimAsync(role.Id, claimType);
            await Context.AddConfirmationAsync();
        }
        [Command("remove")]
        [Priority(10)]
        [Summary("Removes a claim from the role provided.")]
        public async Task RemoveRoleClaimAsync(
            [Summary("The role for the claim to be removed from.")]
                IRole role,
            [Summary("The claim ato be removed from the role.")]
                ClaimMapType claimType)
        {
            await _roleClaimService.RemoveRoleClaimAsync(role.Id, claimType);
            await Context.AddConfirmationAsync();
        }
        [Command]
        [Alias("list")]
        [Summary("Lists a list of role claims for the role provided.")]
        public async Task ListClaimsForRoleAsync(
            [Summary("The role to get the claims of.")]
                IRole role)
        {
            var roleAndClaims = await _roleClaimService.FetchAllClaimsForRoleAsync(role.Id);
            var builder = new StringBuilder();
            foreach(var roleAndClaim in roleAndClaims)
            {
                builder.Append($"{roleAndClaim.Type}, ");
            }
            var embed = new EmbedBuilder()
                .WithTitle($"Role Claims for {role.Name}")
                .WithDescription(builder.ToString())
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}
