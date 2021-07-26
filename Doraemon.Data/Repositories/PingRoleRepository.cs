using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task CreateAsync(PingRoleCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.PingRoles.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        public async Task<PingRole> FetchAsync(ulong roleId)
        {
            return await DoraemonContext.PingRoles
                .FindAsync(roleId);
        }

        public async Task<PingRole> FetchAsync(string roleName)
        {
            return await DoraemonContext.PingRoles
                .Where(x => x.Name == roleName)
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<PingRole>> FetchAllAsync()
        {
            return await DoraemonContext.PingRoles
                .AsQueryable()
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task DeleteAsync(PingRole pingRole)
        {
            DoraemonContext.PingRoles.Remove(pingRole);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}