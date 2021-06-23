﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Data;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;

namespace Doraemon.Data.Services
{
    public class RoleClaimService
    {
        public DoraemonContext _doraemonContext;
        public RoleClaimService(DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
        }
        /// <summary>
        /// Adds a claim to a role, allowing the permissions granted by the claim.
        /// </summary>
        /// <param name="roleId">The ID value of the role to grant the claim.</param>
        /// <param name="claimType">The type of claim to grant the role.</param>
        /// <returns></returns>
        public async Task AddRoleClaimAsync(ulong roleId, ClaimMapType claimType)
        {
            var role = await _doraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claimType)
                .SingleOrDefaultAsync();
            if(role is null)
            {
                _doraemonContext.ClaimMaps.Add(new ClaimMap { RoleId = roleId, Type = claimType });
                await _doraemonContext.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"The role provided already has a claim matching the provided claim.");
            }
        }

        /// <summary>
        /// Removes a claim from the provided role.
        /// </summary>
        /// <param name="roleId">The ID value of the role for the claim to be removed from.</param>
        /// <param name="claimType">The type of claim to remove from the role.</param>
        /// <returns></returns>
        public async Task RemoveRoleClaimAsync(ulong roleId, ClaimMapType claimType)
        {
            var role = await _doraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claimType)
                .SingleOrDefaultAsync();
            if(role is not null)
            {
                throw new InvalidOperationException($"The role provided does not have the claim with that type.");
            }
            _doraemonContext.ClaimMaps.Remove(role);
            await _doraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Returns a list of every role and their claims.
        /// </summary>
        /// <returns><see cref="List{ClaimMap}"/></returns>
        public async Task<List<ClaimMap>> FetchAllRoleClaimsAsync()
        {
            return await _doraemonContext.ClaimMaps.AsQueryable().ToListAsync();
        }

        /// <summary>
        /// Returns a list of claims for the role provided.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for claims.</param>
        /// <returns><see cref="List{ClaimMap}"/></returns>
        public async Task<List<ClaimMap>> FetchAllClaimsForRoleAsync(ulong roleId)
        {
            return await _doraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .ToListAsync();
        }

        /// <summary>
        /// Returns if the role provided has the claim provided.
        /// </summary>
        /// <param name="roleId">The ID of the role to check.</param>
        /// <param name="claimType">The type of claim to check.</param>
        /// <returns><see cref="bool"/></returns>
        public async Task<bool> RoleHasClaimAsync(ulong roleId, ClaimMapType claimType)
        {
            var role = await _doraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claimType)
                .SingleOrDefaultAsync();
            return role is not null;
        }

        public async Task AutoConfigureGuildAsync(IEnumerable<SocketRole> roles)
        {
            foreach(var role in roles)
            {
                await AddRoleClaimAsync(role.Id, ClaimMapType.AuthorizationManage);
            }
        }
    }
}
