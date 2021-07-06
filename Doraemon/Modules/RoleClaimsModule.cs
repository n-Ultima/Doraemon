using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.CommandHelp;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Humanizer;

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
        private readonly RoleClaimService _roleClaimService;

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
                params ClaimMapType[] claimType)
        {
            foreach (var claim in claimType) await _roleClaimService.AddRoleClaimAsync(role.Id, Context.User.Id, claim);
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
            await _roleClaimService.RemoveRoleClaimAsync(role.Id, Context.User.Id, claimType);
            await Context.AddConfirmationAsync();
        }

        [Command]
        [Alias("list")]
        [Summary("Lists a list of role claims for the role provided.")]
        public async Task ListClaimsForRoleAsync(
            [Summary("The role to get the claims of.")]
                IRole role)
        {
            var list = new List<string>();
            var roleAndClaims = await _roleClaimService.FetchAllClaimsForRoleAsync(role.Id);
            var embed = new EmbedBuilder()
                .WithTitle($"Role Claims for {role.Name}")
                .WithDescription(roleAndClaims.Humanize())
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}