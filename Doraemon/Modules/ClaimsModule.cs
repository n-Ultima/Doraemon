using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common.CommandHelp;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Humanizer;

namespace Doraemon.Modules
{
    [Name("Claims")]
    [RequireContext(ContextType.Guild)]
    [Summary("Provides helpful configuration for managing claims to users and roles.")]
    [HelpTags("claim", "role claim", "roles")]
    public class ClaimsModule : ModuleBase
    {
        private readonly ClaimService _claimService;

        public ClaimsModule(ClaimService claimService)
        {
            _claimService = claimService;
        }

        [Command("auth claims add")]
        [Alias("auth claims grant", "auth claims allow")]
        [Priority(10)]
        [Summary("Adds a claim to the given role.")]
        public async Task AddRoleClaimAsync(
            [Summary("The role for the claim to be granted.")]
                IRole role,
            [Summary("The claim to be added.")] 
                params ClaimMapType[] claimType)
        {
            foreach (var claim in claimType) await _claimService.AddRoleClaimAsync(role.Id, claim);
            await Context.AddConfirmationAsync();
        }

        [Command("auth claims remove")]
        [Alias("auth claims revoke", "auth claims disallow")]
        [Priority(10)]
        [Summary("Removes a claim from the role provided.")]
        public async Task RemoveRoleClaimAsync(
            [Summary("The role for the claim to be removed from.")]
                IRole role,
            [Summary("The claim ato be removed from the role.")]
                ClaimMapType claimType)
        {
            await _claimService.RemoveRoleClaimAsync(role.Id, Context.User.Id, claimType);
            await Context.AddConfirmationAsync();
        }

        [Command("auth claims add")]
        [Alias("auth claims grant", "auth claims allow")]
        [Summary("Add claims to the given user.")]
        public async Task AddUserClaimAsync(
            [Summary("The user to add the claim to.")]
                SocketGuildUser user,
            [Summary("The claims to add to the user.")]
                params ClaimMapType[] claims)
        {
            foreach (var claim in claims)
            {
                await _claimService.AddUserClaimAsync(user.Id, claim);
            }

            await Context.AddConfirmationAsync();
        }

        [Command("auth claims remove")]
        [Alias("auth claims revoke", "auth claims disallow")]
        [Summary("Removes the given claim from the user provided.")]
        public async Task RemoveUserClaimAsync(
            [Summary("The user to have the claim removed from.")]
                SocketGuildUser user,
            [Summary("The claim to remove")] 
                ClaimMapType claim)
        {
            await _claimService.RemoveUserClaimAsync(user.Id, claim);
            await Context.AddConfirmationAsync();
        }

        [Command("auth claims")]
        [Summary("Fetches all the claims that the user posesses.")]

        public async Task FetchAuthClaimsAsync(
            [Summary("The user to fetch claims for.")]
                SocketGuildUser user)
        {
            var totalClaims = await _claimService.FetchAllClaimsForUserAsync(user.Id);
            if (!totalClaims.Any())
            {
                await ReplyAsync($"No claims assigned.");
                return;
            }

            
            var humanizedClaims = string.Join("\n", totalClaims);
            await ReplyAsync($"```\n{humanizedClaims}\n```");

        }

        [Command("auth claims")]
        [Alias("auth claims list")]
        [Summary("Fetches all claims for the given role.")]

        public async Task FetchAuthClaimsAsync(
            [Summary("The role to fetch claims for.")]
                IRole role)
        {
            var totalClaims = await _claimService.FetchAllClaimsForRoleAsync(role.Id);
            if (!totalClaims.Any())
            {
                await ReplyAsync($"No claims assigned.");
                return;
            }
            var humanizedClaims = string.Join("\n", totalClaims);
            await ReplyAsync($"```\n{humanizedClaims}\n```");
        }
    }
}