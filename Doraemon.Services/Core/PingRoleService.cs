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

        public async Task AddPingRoleAsync(ulong Id, ulong requestorId, string name)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);

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

        public async Task<PingRole> FetchPingRoleAsync(ulong roleId)
        {
            return await _pingRoleRepository.FetchAsync(roleId);
        }

        public async Task<PingRole> FetchPingRoleAsync(string roleName)
        {
            return await _pingRoleRepository.FetchAsync(roleName);
        }

        public async Task<IEnumerable<PingRole>> FetchAllPingRolesAsync()
        {
            return await _pingRoleRepository.FetchAllAsync();
        }

        public async Task RemovePingRoleAsync(ulong requestorId, ulong roleId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);

            var pingRole = await _pingRoleRepository.FetchAsync(roleId);

            if (pingRole is null) throw new InvalidOperationException("The role ID provided is not a pingrole.");

            await _pingRoleRepository.DeleteAsync(pingRole);
        }
    }
}