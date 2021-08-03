﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class PingRoleService
    {
        private readonly AuthorizationService _authorizationService;

        private readonly PingRoleRepository _pingRoleRepository;

        public PingRoleService(PingRoleRepository pingRoleRepository, AuthorizationService authorizationService)
        {
            _pingRoleRepository = pingRoleRepository;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Adds a currently existing role to the list of pingroles.
        /// </summary>
        /// <param name="Id">The ID value of the role.</param>
        /// <param name="name">The name of the role.</param>
        /// <exception cref="ArgumentException"></exception>
        public async Task AddPingRoleAsync(Snowflake Id, string name)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildManage);

            var role = await _pingRoleRepository.FetchAsync(name);
            if (role is not null)
            {
                throw new ArgumentException($"The role provided is already a pingrole.");
            }

            using (var transaction = await _pingRoleRepository.BeginCreateTransactionAsync())
            {
                await _pingRoleRepository.CreateAsync(new PingRoleCreationData
                {
                    Id = Id,
                    Name = name
                });
                transaction.Commit();
            }
        }

        /// <summary>
        /// Fetches a pingrole.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="PingRole"/> with the given ID.</returns>
        public async Task<PingRole> FetchPingRoleAsync(Snowflake roleId)
        {
            return await _pingRoleRepository.FetchAsync(roleId);
        }

        /// <summary>
        /// Fetches a pingrole.
        /// </summary>
        /// <param name="roleName">The name of the role to query for.</param>
        /// <returns>A <see cref="PingRole"/> with the given name.</returns>
        public async Task<PingRole> FetchPingRoleAsync(string roleName)
        {
            return await _pingRoleRepository.FetchAsync(roleName);
        }

        /// <summary>
        /// Returns a list of all currently-existing pingroles.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{PingRole}"/>.</returns>
        public async Task<IEnumerable<PingRole>> FetchAllPingRolesAsync()
        {
            return await _pingRoleRepository.FetchAllAsync();
        }

        /// <summary>
        /// Removes a pingrole from the list of pingroles.
        /// </summary>
        /// <param name="roleId">The ID value of the role.</param>
        /// <exception cref="InvalidOperationException">Thrown if the role ID provided is not a pingrole.</exception>
        public async Task RemovePingRoleAsync(Snowflake roleId)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildManage);

            var pingRole = await _pingRoleRepository.FetchAsync(roleId);

            if (pingRole is null) throw new Exception("The role ID provided is not a pingrole.");

            await _pingRoleRepository.DeleteAsync(pingRole);
        }
    }
}