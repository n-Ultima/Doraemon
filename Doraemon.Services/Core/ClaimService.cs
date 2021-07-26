using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class ClaimService
    {
        private readonly AuthorizationService _authorizationService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private readonly ClaimMapRepository _claimMapRepository;
        private readonly DiscordSocketClient _client;

        public ClaimService(AuthorizationService authorizationService, ClaimMapRepository claimMapRepository, DiscordSocketClient client)
        {
            _authorizationService = authorizationService;
            _claimMapRepository = claimMapRepository;
            _client = client;
        }

        /// <summary>
        ///     Adds a claim to a role, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="roleId">The ID value of the role to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the role.</param>
        /// <returns></returns>
        public async Task AddRoleClaimAsync(ulong roleId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            if (await _claimMapRepository.FetchSingleRoleClaimAsync(roleId, claimType) is not null)
                throw new InvalidOperationException($"That role already has the `{claimType}` claim.");
            await _claimMapRepository.CreateAsync(new RoleClaimMapCreationData()
            {
                RoleId = roleId,
                Type = claimType
            });
        }

        /// <summary>
        /// Adds a claim to a user, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="userId">The ID value of the user to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the user.</param>
        /// <returns></returns>
        public async Task AddUserClaimAsync(ulong userId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            if (await _claimMapRepository.FetchSingleUserClaimAsync(userId, claimType) is not null)
                throw new ArgumentException($"That user already has the `{claimType}` claim.");
            await _claimMapRepository.CreateAsync(new UserClaimMapCreationData()
            {
                UserId = userId,
                Type = claimType
            });

        }
        
        /// <summary>
        /// Returns a users claims. This also includes claims contained by the user's roles.
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/></returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForUserAsync(ulong userId)
        {
            return await _claimMapRepository.FetchAllClaimsForUserAsync(userId);
        }
        
        /// <summary>
        /// Removes a claim from the provided user.
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <param name="claimType">The claim to be removed from the user.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task RemoveUserClaimAsync(ulong userId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            var user = await _claimMapRepository.FetchSingleUserClaimAsync(userId, claimType);
            if (user is null)
                throw new ArgumentNullException($"The user provided does not have that claim.");
            await _claimMapRepository.DeleteAsync(user);
        }
        /// <summary>
        ///     Removes a claim from the provided role.
        /// </summary>
        /// <param name="roleId">The ID value of the role for the claim to be removed from.</param>
        /// <param name="claimType">The type of claim to remove from the role.</param>
        /// <returns></returns>
        public async Task RemoveRoleClaimAsync(ulong roleId, ulong requestorId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            var role = await _claimMapRepository.FetchSingleRoleClaimAsync(roleId, claimType);
            if (role is null)
                throw new ArgumentException("The role provided does not have the claim with that type.");
            await _claimMapRepository.DeleteAsync(role);
        }

        /// <summary>
        ///     Returns a list of claims for the role provided.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for claims.</param>
        /// <returns>
        ///     <see cref="List{ClaimMap}" />
        /// </returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForRoleAsync(ulong roleId)
        {
            return await _claimMapRepository.FetchAllClaimsForRoleAsync(roleId);
        }

        /// <summary>
        ///     Returns if the user provided has the claim provided.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="type">The type of claim to check.</param>
        /// <returns>
        /// <see cref="bool" />
        /// </returns>
        public async Task<bool> UserHasClaimAsync(ulong userId, ClaimMapType type)
        {
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var gUser = guild.GetUser(userId);
            var roles = gUser.Roles.Select(x => x.Id);
            var allClaims = await _claimMapRepository.RetrievePossessedClaimsAsync(userId, roles);
            return allClaims.Contains(type);
        }
    }
}