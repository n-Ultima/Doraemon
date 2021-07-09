using System.Collections.Generic;
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

        [Command("role claims add")]
        [Priority(10)]
        [Summary("Adds a claim to the given role.")]
        public async Task AddRoleClaimAsync(
            [Summary("The role for the claim to be granted.")]
                IRole role,
            [Summary("The claim to be added.")] 
                params ClaimMapType[] claimType)
        {
            foreach (var claim in claimType) await _claimService.AddRoleClaimAsync(role.Id, Context.User.Id, claim);
            await Context.AddConfirmationAsync();
        }

        [Command("role claims remove")]
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

        [Command("user claims add")]
        [Summary("Add claims to the given user.")]
        public async Task AddUserClaimAsync(
            [Summary("The user to add the claim to.")]
                SocketGuildUser user,
            [Summary("The claims to add to the user.")]
                params ClaimMapType[] claims)
        {
            foreach (var claim in claims)
            {
                await _claimService.AddUserClaimAsync(user.Id, Context.User.Id, claim);
            }

            await Context.AddConfirmationAsync();
        }

        [Command("user claims remove")]
        [Summary("Removes the given claim from the user provided.")]
        public async Task RemoveUserClaimAsync(
            [Summary("The user to have the claim removed from.")]
                SocketGuildUser user,
            [Summary("The claim to remove")] 
                ClaimMapType claim)
        {
            await _claimService.RemoveUserClaimAsync(user.Id, Context.User.Id, claim);
            await Context.AddConfirmationAsync();
        }
        [Command("list -r")]
        [Summary("Lists a list of role claims for the role provided.")]
        public async Task ListClaimsForRoleAsync(
            [Summary("The role to get the claims of.")]
            IRole role)
        {
            var roleAndClaims = await _claimService.FetchAllClaimsForRoleAsync(role.Id);
            var embed = new EmbedBuilder()
                .WithTitle($"Role Claims for {role.Name}")
                .WithDescription(roleAndClaims.Humanize())
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("list -u")]
        [Summary("List all the claims that the given user has.")]

        public async Task ListClaimsForUserAsync(
            [Summary("The user to get the claims of.")]
            SocketGuildUser user)
        {
            var userAndClaims = await _claimService.FetchUserClaimsAsync(user.Id);
            var embed = new EmbedBuilder()
                .WithTitle($"Usr Claims for {user.GetFullUsername()}")
                .WithDescription(userAndClaims.Humanize())
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}