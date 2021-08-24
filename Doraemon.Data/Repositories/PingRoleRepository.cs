using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class PingRoleRepository : Repository
    {
        public PingRoleRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        /// Creates a new <see cref="PingRole"/> with the specified <see cref="PingRoleCreationData"/>
        /// </summary>
        /// <param name="data">The needed <see cref="PingRoleCreationData"/> used to construct a new <see cref="PingRole"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null</exception>
        public async Task CreateAsync(PingRoleCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.PingRoles.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Fetches a PingRole by Id.
        /// </summary>
        /// <param name="roleId">The ID value of the role.</param>
        /// <returns>A <see cref="PingRole"/> with the specified ID.</returns>
        public async Task<PingRole> FetchAsync(Snowflake roleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.PingRoles
                    .FindAsync(roleId);
            }
        }

        /// <summary>
        /// Fetches a PingRole by it's name.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>A <see cref="PingRole"/> with the specified </returns>
        public async Task<PingRole> FetchAsync(string roleName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.PingRoles
                    .Where(x => x.Name == roleName)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        /// Fetches a list of all existing PingRoles.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{PingRole}"/></returns>
        public async Task<IEnumerable<PingRole>> FetchAllAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.PingRoles
                    .AsQueryable()
                    .OrderBy(x => x.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Deletes a PingRole from the database.
        /// </summary>
        /// <param name="pingRole">The PingRole to delete.</param>
        public async Task DeleteAsync(PingRole pingRole)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.PingRoles.Remove(pingRole);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}