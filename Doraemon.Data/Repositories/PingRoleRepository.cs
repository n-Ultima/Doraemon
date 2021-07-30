using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class PingRoleRepository : Repository
    {
        public PingRoleRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {
        }
        private static readonly RepositoryTransactionFactory _createTransactionFactory = new RepositoryTransactionFactory();
        public Task<IRepositoryTransaction> BeginCreateTransactionAsync()
            => _createTransactionFactory.BeginTransactionAsync(DoraemonContext.Database);
        /// <summary>
        /// Creates a new <see cref="PingRole"/> with the specified <see cref="PingRoleCreationData"/>
        /// </summary>
        /// <param name="data">The needed <see cref="PingRoleCreationData"/> used to construct a new <see cref="PingRole"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null</exception>
        public async Task CreateAsync(PingRoleCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.PingRoles.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Fetches a PingRole by Id.
        /// </summary>
        /// <param name="roleId">The ID value of the role.</param>
        /// <returns></returns>
        public async Task<PingRole> FetchAsync(Snowflake roleId)
        {
            return await DoraemonContext.PingRoles
                .FindAsync(roleId);
        }

        /// <summary>
        /// Fetches a PingRole by it's name.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns></returns>
        public async Task<PingRole> FetchAsync(string roleName)
        {
            return await DoraemonContext.PingRoles
                .Where(x => x.Name == roleName)
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Fetches a list of all existing PingRoles.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{PingRole}"/></returns>
        public async Task<IEnumerable<PingRole>> FetchAllAsync()
        {
            return await DoraemonContext.PingRoles
                .AsQueryable()
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Deletes a PingRole from the database.
        /// </summary>
        /// <param name="pingRole">The PingRole to delete.</param>
        public async Task DeleteAsync(PingRole pingRole)
        {
            DoraemonContext.PingRoles.Remove(pingRole);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}