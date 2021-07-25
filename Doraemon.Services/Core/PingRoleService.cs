using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class PingRoleService
    {
        private readonly AuthorizationService _authorizationService;

        private readonly DiscordSocketClient _client;
        private readonly PingRoleRepository _pingRoleRepository;

        public PingRoleService(PingRoleRepository pingRoleRepository, DiscordSocketClient client, AuthorizationService authorizationService)
        {
            _pingRoleRepository = pingRoleRepository;
            _client = client;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Adds a currently existing role to the list of pingroles.
        /// </summary>
        /// <param name="Id">The ID value of the role.</param>
        /// <param name="requestorId">The user requesting the action.</param>
        /// <param name="name">The name of the role.</param>
        /// <exception cref="ArgumentException"></exception>
        public async Task AddPingRoleAsync(ulong Id, string name)
        {
            await _authorizationService.RequireClaims(ClaimMapType.GuildManage);

            var role = await _pingRoleRepository.FetchAsync(name);
            if (role is not null)
            {
                throw new ArgumentException($"The role provided is already a pingrole.");
            }
            await _pingRoleRepository.CreateAsync(new PingRoleCreationData
            {
                Id = Id,
                Name = name
            });
        }

        /// <summary>
        /// Fetches a pingrole.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="PingRole"/> with the given ID.</returns>
        public async Task<PingRole> FetchPingRoleAsync(ulong roleId)
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
        public async Task RemovePingRoleAsync(ulong roleId)
        {
            await _authorizationService.RequireClaims(ClaimMapType.GuildManage);

            var pingRole = await _pingRoleRepository.FetchAsync(roleId);

            if (pingRole is null) throw new InvalidOperationException("The role ID provided is not a pingrole.");

            await _pingRoleRepository.DeleteAsync(pingRole);
        }
    }
}