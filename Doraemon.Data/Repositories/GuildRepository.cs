using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class GuildRepository : Repository
    {
        public GuildRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        ///     Creates a new <see cref="Guild" /> with the provided <see cref="GuildCreationData" />.
        /// </summary>
        /// <param name="data">The data needed to construct a new <see cref="Guild" /> object.</param>
        /// <returns></returns>
        public async Task CreateAsync(GuildCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.Guilds.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Fetches a guild.
        /// </summary>
        /// <param name="guildId">The guild's ID to fetch from the database.</param>
        /// <returns></returns>
        public async Task<Guild> FetchGuildAsync(string guildId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Guilds
                    .FindAsync(guildId);   
            }
        }

        /// <summary>
        ///     Fetches all guilds currently present on the whitelist.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Guild}"/> containing all whitelisted guilds.</returns>
        public async Task<IEnumerable<Guild>> FetchAllWhitelistedGuildsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Guilds
                    .AsQueryable()
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <summary>
        ///     Deletes a <see cref="Guild" /> from the database.
        /// </summary>
        /// <param name="guild">The guild to remove.</param>
        /// <returns></returns>
        public async Task DeleteAsync(Guild guild)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.Guilds.Remove(guild);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}