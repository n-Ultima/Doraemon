using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    public class ClaimMapRepository : Repository
    {
        public ClaimMapRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {
        }

        /// <summary>
        ///     Creates a role claim entity with the specified <see cref="ClaimMapCreationData" />.
        /// </summary>
        /// <param name="data">The objects needed to construct a <see cref="ClaimMap" />.</param>
        /// <returns></returns>
        public async Task CreateAsync(ClaimMapCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.ClaimMaps.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Deletes a role claim.
        /// </summary>
        /// <param name="claim">The <see cref="ClaimMap" /> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(ClaimMap claim)
        {
            DoraemonContext.ClaimMaps.Remove(claim);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Fetches all claims for a role.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}" />.</returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForRoleAsync(ulong roleId)
        {
            return await DoraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Select(x => x.Type)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForUserAsync(ulong userId)
        {
            return await DoraemonContext.ClaimMaps
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
        public async Task<ClaimMap> FetchSingleRoleClaimAsync(ulong roleId, ClaimMapType claim)
        {
            return await DoraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claim)
                .SingleOrDefaultAsync();
        }

        public async Task<ClaimMap> FetchSingleUserClaimAsync(ulong userId, ClaimMapType claim)
        {
            return await DoraemonContext.ClaimMaps
                .Where(x => x.UserId == userId)
                .Where(x => x.Type == claim)
                .SingleOrDefaultAsync();
        }
    }
}