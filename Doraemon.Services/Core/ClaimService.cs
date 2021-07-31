using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class ClaimService : DiscordBotService
    {
        private readonly AuthorizationService _authorizationService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private readonly ClaimMapRepository _claimMapRepository;

        public ClaimService(AuthorizationService authorizationService, ClaimMapRepository claimMapRepository)
        {
            _authorizationService = authorizationService;
            _claimMapRepository = claimMapRepository;
        }

        /// <summary>
        ///     Adds a claim to a role, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="roleId">The ID value of the role to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the role.</param>
        /// <returns></returns>
        public async Task AddRoleClaimAsync(Snowflake roleId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            if (await _claimMapRepository.FetchSingleRoleClaimAsync(roleId, claimType) is not null)
                throw new InvalidOperationException($"That role already has the `{claimType}` claim.");
            using (var transaction = await _claimMapRepository.BeginCreateTransactionAsync())
            {
                await _claimMapRepository.CreateAsync(new RoleClaimMapCreationData()
                {
                    RoleId = roleId,
                    Type = claimType
                });
                transaction.Commit();
            }
        }

        /// <summary>
        /// Adds a claim to a user, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="userId">The ID value of the user to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the user.</param>
        /// <returns></returns>
        public async Task AddUserClaimAsync(Snowflake userId, ClaimMapType claimType)
        {
            _authorizationService.RequireClaims(ClaimMapType.AuthorizationManage);
            if (await _claimMapRepository.FetchSingleUserClaimAsync(userId, claimType) is not null)
                throw new ArgumentException($"That user already has the `{claimType}` claim.");
            using (var transaction = await _claimMapRepository.BeginCreateTransactionAsync())
            {
                await _claimMapRepository.CreateAsync(new UserClaimMapCreationData()
                {
                    UserId = userId,
                    Type = claimType
                });
                transaction.Commit();
            }

        }
        
        /// <summary>
        /// Returns a users claims. This also includes claims contained by the user's roles.
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <param name="roleIds"> The set of roleIds that the user has.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/></returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForUserAsync(Snowflake userId, params Snowflake[] roleIds)
        {
            return await _claimMapRepository.RetrievePossessedClaimsAsync(userId, roleIds);
        }
        
        /// <summary>
        /// Removes a claim from the provided user.
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <param name="claimType">The claim to be removed from the user.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task RemoveUserClaimAsync(Snowflake userId, ClaimMapType claimType)
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
        public async Task RemoveRoleClaimAsync(Snowflake roleId,ClaimMapType claimType)
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
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForRoleAsync(Snowflake roleId)
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
        public async Task<bool> UserHasClaimAsync(Snowflake userId, ClaimMapType type)
        {
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var gUser = guild.GetMember(userId);
            var roles = gUser.RoleIds;
            var allClaims = await _claimMapRepository.RetrievePossessedClaimsAsync(userId, roles);
            return allClaims.Contains(type);
        }
    }
}