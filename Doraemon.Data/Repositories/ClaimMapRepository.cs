using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class ClaimMapRepository : RepositoryVersionTwo
    {

        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public ClaimMapRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.RoleClaimMaps.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.UserClaimMaps.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes a user claim
        /// </summary>
        /// <param name="claim">The claim to delete.</param>
        public async Task DeleteAsync(UserClaimMap claim)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.UserClaimMaps.Remove(claim);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Deletes a role claim.
        /// </summary>
        /// <param name="claim">The <see cref="RoleClaimMap" /> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(RoleClaimMap claim)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.RoleClaimMaps.Remove(claim);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Fetches all claims for a role.
        /// </summary>
        /// <param name="roleId">The ID value of the role to query for.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}" />.</returns>
        public async Task<IEnumerable<ClaimMapType>> FetchAllClaimsForRoleAsync(Snowflake roleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.RoleClaimMaps
                    .Where(x => x.RoleId == roleId)
                    .AsNoTracking()
                    .Select(x => x.Type)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Returns a list of claims that the user contains, ignoring role claims that the user possesses.
        /// </summary>
        /// <param name="userId">The ID value of the user to fetch their exclusive claims.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/> that contains the claim for the user.</returns>
        public async Task<IEnumerable<ClaimMapType>> FetchUserExclusiveClaimsAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.UserClaimMaps
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Type)
                    .ToListAsync();
            }
        }

        /// <summary>
        ///     Fetches a single role claim.
        /// </summary>
        /// <param name="roleId">The role to query for.</param>
        /// <param name="claim">The <see cref="ClaimMapType" /> to query for alongside the role.</param>
        /// <returns>A <see cref="ClaimMap" /></returns>
        public async Task<RoleClaimMap> FetchSingleRoleClaimAsync(Snowflake roleId, ClaimMapType claim)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.RoleClaimMaps
                    .Where(x => x.RoleId == roleId)
                    .Where(x => x.Type == claim)
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Fetches a single claim that a user may possess.
        /// </summary>
        /// <param name="userId">The userID to query for.</param>
        /// <param name="claim">The <see cref="UserClaimMap"/> provided.</param>
        /// <returns></returns>
        public async Task<UserClaimMap> FetchSingleUserClaimAsync(ulong userId, ClaimMapType claim)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.UserClaimMaps
                    .Where(x => x.UserId == userId)
                    .Where(x => x.Type == claim)
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Fetches a list of claims for the user, including roles possessed claims.
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <param name="roleIds">The ID values of the roles that the user contains.</param>
        /// <returns>A <see cref="IEnumerable{ClaimMapType}"/> containing all of the users claims.</returns>
        public async Task<IEnumerable<ClaimMapType>> RetrievePossessedClaimsAsync(Snowflake userId, IEnumerable<Snowflake> roleIds)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                List<ClaimMapType> currentClaims = new();
                var userClaims = await FetchUserExclusiveClaimsAsync(userId);
                currentClaims.AddRange(userClaims);
                var roleClaims = await doraemonContext.RoleClaimMaps
                    .FilterBy(new RoleClaimMapSearchCriteria()
                    {
                        RoleIds = roleIds,
                    })
                    .Select(x => x.Type)
                    .ToListAsync();
                foreach (var roleAndClaim in roleClaims)
                {
                    if (currentClaims.Contains(roleAndClaim))
                        continue;
                    currentClaims.Add(roleAndClaim);
                }

                return currentClaims;
            }
        }
    }
}