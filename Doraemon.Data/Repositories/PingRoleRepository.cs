using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Data.Repositories
{
    public class PingRoleRepository : Repository
    {
        public PingRoleRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        { }

        public async Task CreateAsync(PingRoleCreationData data)
        {
            if(data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
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
                .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<PingRole>> FetchAllAsync()
        {
            return await DoraemonContext.PingRoles
                .AsQueryable()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task DeleteAsync(PingRole pingRole)
        {
            DoraemonContext.PingRoles.Remove(pingRole);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}
