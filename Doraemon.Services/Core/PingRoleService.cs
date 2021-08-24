using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class PingRoleService : DoraemonBotService
    {
        private readonly AuthorizationService _authorizationService;


        public PingRoleService(IServiceProvider serviceProvider, AuthorizationService authorizationService)
         : base(serviceProvider)
        {
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
            _authorizationService.RequireClaims(ClaimMapType.GuildPingRoleManage);

            using (var scope = ServiceProvider.CreateScope())
            {
                var pingRoleRepository = scope.ServiceProvider.GetRequiredService<PingRoleRepository>();
                var role = await pingRoleRepository.FetchAsync(name);
                if (role is not null)
                {
                    throw new ArgumentException($"The role provided is already a pingrole.");
                }

                await pingRoleRepository.CreateAsync(new PingRoleCreationData
                {
                    Id = Id,
                    Name = name
                });   
            }
        }

        /// <summary>
        /// Fetches a pingrole.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="PingRole"/> with the given ID.</returns>
        public async Task<PingRole> FetchPingRoleAsync(Snowflake roleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var pingRoleRepository = scope.ServiceProvider.GetRequiredService<PingRoleRepository>();
                return await pingRoleRepository.FetchAsync(roleId);

            }
        }

        /// <summary>
        /// Fetches a pingrole.
        /// </summary>
        /// <param name="roleName">The name of the role to query for.</param>
        /// <returns>A <see cref="PingRole"/> with the given name.</returns>
        public async Task<PingRole> FetchPingRoleAsync(string roleName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var pingRoleRepository = scope.ServiceProvider.GetRequiredService<PingRoleRepository>();
                return await pingRoleRepository.FetchAsync(roleName);

            }
        }

        /// <summary>
        /// Returns a list of all currently-existing pingroles.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{PingRole}"/>.</returns>
        public async Task<IEnumerable<PingRole>> FetchAllPingRolesAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var pingRoleRepository = scope.ServiceProvider.GetRequiredService<PingRoleRepository>();
                return await pingRoleRepository.FetchAllAsync();
            }
        }

        /// <summary>
        /// Removes a pingrole from the list of pingroles.
        /// </summary>
        /// <param name="roleId">The ID value of the role.</param>
        /// <exception cref="InvalidOperationException">Thrown if the role ID provided is not a pingrole.</exception>
        public async Task RemovePingRoleAsync(Snowflake roleId)
        {
            _authorizationService.RequireClaims(ClaimMapType.GuildPingRoleManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var pingRoleRepository = scope.ServiceProvider.GetRequiredService<PingRoleRepository>();
                var pingRole = await pingRoleRepository.FetchAsync(roleId);

                if (pingRole is null) throw new Exception("The role ID provided is not a pingrole.");

                await pingRoleRepository.DeleteAsync(pingRole);
            }
        }
    }
}