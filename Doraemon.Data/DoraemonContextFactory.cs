using Doraemon.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Doraemon.Data
{
    public class DoraemonContextFactory : IDesignTimeDbContextFactory<DoraemonContext>
    {
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public DoraemonContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder();
            options.UseNpgsql(DoraemonConfig.DbConnection);
            return new DoraemonContext(options.Options);
        }
    }
}