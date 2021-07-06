using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Doraemon.Data
{
    public class DoraemonContextFactory : IDesignTimeDbContextFactory<DoraemonContext>
    {
        public DoraemonContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();
            var options = new DbContextOptionsBuilder();
            options.UseNpgsql(config["DbConnection"]);
            return new DoraemonContext(options.Options);
        }
    }
}