using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class ClaimMapRepository : Repository
    {
        private readonly DiscordSocketClient _client;

        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ClaimMapRepository(DoraemonContext doraemonContext, DiscordSocketClient client)
            : base(doraemonContext)
        {
            _client = client;
        }

        /// <summary>
        ///     Creates a role claim entity with the specified <see cref="RoleClaimMapCreationData" />.
        /// </summary>
        /// <param name="data">The objects needed to construct a <see cref="RoleClaimMap" />.</param>
        /// <returns></returns>
        public async Task CreateAsync(RoleClaimMapCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.RoleClaimMaps.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a user claim entity with the specified <see cref="UserClaimMapCreationData"/>.
        /// </summary>
        /// <param name="data">The objects needed to construct a <see cref="UserClaimMap"/></param>
        /// <returns></returns>
        public async Task CreateAsync(UserClaimMapCreationData data)
        {
            if (data is null) throw new ArgumentException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.UserClaimMaps.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a user claim
        /// </summary>
        /// <param name="claim"></param>
        public async Task DeleteAsync(UserClaimMap claim)
        {
            DoraemonContext.UserClaimMaps.Remove(claim);
            await DoraemonContext.SaveChangesAsync();
        }
        /// <summary>
        ///     Deletes a role claim.
        /// </summary>
        /// <param name="claim">The <see cref="RoleClaimMap" /> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(RoleClaimMap claim)
        {
            DoraemonContext.RoleClaimMaps.Remove(claim);
            await DoraemonContext.SaveChangesAsync();
        }
        
        /// <summary>
        ///     Fetches all claims for a role.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}" />.</returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForRoleAsync(ulong roleId)
        {
            return await DoraemonContext.RoleClaimMaps
                .Where(x => x.RoleId == roleId)
                .Select(x => x.Type)
                .ToListAsync();
        }

        /// <summary>
        /// Fetches a list of claims that a user has, this also includes role claims that the user posesses.
        /// </summary>
        /// <param name="userId">The userID to query for.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/></returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForUserAsync(ulong userId)
        {
            List<ClaimMapType> totalClaims = new();
            var singleUserClaims =  await DoraemonContext.UserClaimMaps
                .Where(x => x.UserId == userId)
                .Select(x => x.Type)
                .ToListAsync();
            totalClaims.AddRange(singleUserClaims);
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var gUser = guild.GetUser(userId);
            if (gUser is null) return null;
            var roles = gUser.Roles.OrderByDescending(x => x.Position);
            foreach (var role in roles)
            {
                var check = await FetchAllClaimsForRoleAsync(role.Id);
                foreach (var claim in check)
                {
                    if (totalClaims.Contains(claim))
                    {
                        Log.Logger.Information($"Skipping claim due to redundance.");
                    }
                    else
                    {
                        totalClaims.Add(claim);
                    }
                }
            }

            return totalClaims;

        }
        
        /// <summary>
        /// Returns a list of claims that the user contains, ignoring role claims that the user posesses.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/></returns>
        public async Task<IEnumerable<ClaimMapType>> FetchUserExclusiveClaimsAsync(ulong userId)
        {
            return await DoraemonContext.UserClaimMaps
                .Where(x => x.UserId == userId)
                .Select(x => x.Type)
                .ToListAsync();
        }
        /// <summary>
        ///     Fetches a single role claim.
        /// </summary>
        /// <param name="roleId">The role to query for.</param>
        /// <param name="claim">The <see cref="ClaimMapType" /> to query for alongside the role.</param>
        /// <returns>A <see cref="ClaimMap" /></returns>
        public async Task<RoleClaimMap> FetchSingleRoleClaimAsync(ulong roleId, ClaimMapType claim)
        {
            return await DoraemonContext.RoleClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claim)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Fetches a single claim that a user may possess.
        /// </summary>
        /// <param name="userId">The userID to query for.</param>
        /// <param name="claim">The <see cref="UserClaimMap"/> provided.</param>
        /// <returns></returns>
        public async Task<UserClaimMap> FetchSingleUserClaimAsync(ulong userId, ClaimMapType claim)
        {
            return await DoraemonContext.UserClaimMaps
                .Where(x => x.UserId == userId)
                .Where(x => x.Type == claim)
                .SingleOrDefaultAsync();
        }

        
    }
}