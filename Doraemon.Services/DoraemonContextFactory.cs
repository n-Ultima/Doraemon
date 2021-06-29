
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Doraemon.Data.Models;

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