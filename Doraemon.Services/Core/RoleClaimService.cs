using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    public class RoleClaimService
    {
        public AuthorizationService _authorizationService;
        public ClaimMapRepository _claimMapRepository;
        public DiscordSocketClient _client;

        public RoleClaimService(AuthorizationService authorizationService, DiscordSocketClient client,
            ClaimMapRepository claimMapRepository)
        {
            _authorizationService = authorizationService;
            _client = client;
            _claimMapRepository = claimMapRepository;
        }

        /// <summary>
        ///     Adds a claim to a role, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="roleId">The ID value of the role to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the role.</param>
        /// <returns></returns>
        public async Task AddRoleClaimAsync(ulong roleId, ulong requestorId, ClaimMapType claimType)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.AuthorizationManage);
            if (await _claimMapRepository.FetchSingleRoleClaimAsync(roleId, claimType) is null)
                throw new InvalidOperationException($"That role already has the `{claimType}` claim.");
            await _claimMapRepository.CreateAsync(new ClaimMapCreationData
            {
                RoleId = roleId,
                Type = claimType
            });
        }

        /// <summary>
        ///     Removes a claim from the provided role.
        /// </summary>
        /// <param name="roleId">The ID value of the role for the claim to be removed from.</param>
        /// <param name="claimType">The type of claim to remove from the role.</param>
        /// <returns></returns>
        public async Task RemoveRoleClaimAsync(ulong roleId, ulong requestorId, ClaimMapType claimType)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.AuthorizationManage);
            var role = await _claimMapRepository.FetchSingleRoleClaimAsync(roleId, claimType);
            if (role is null)
                throw new InvalidOperationException("The role provided does not have the claim with that type.");
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
        ///     Returns if the role provided has the claim provided.
        /// </summary>
        /// <param name="roleId">The ID of the role to check.</param>
        /// <param name="claimType">The type of claim to check.</param>
        /// <returns>
        ///     <see cref="bool" />
        /// </returns>
        public async Task AutoConfigureGuildAsync(IEnumerable<SocketRole> roles)
        {
            foreach (var role in roles)
                await AddRoleClaimAsync(role.Id, _client.CurrentUser.Id, ClaimMapType.AuthorizationManage);
        }
    }
}