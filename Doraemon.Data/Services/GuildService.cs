using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Services
{
    public class GuildService
    {
        public DoraemonContext _doraemonContext;
        public GuildService(DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
        }
        /// <summary>
        /// Adds a guild to the whitelist. A whitelisted guild's invites will not be moderated by the bot.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="guildName"></param>
        /// <returns></returns>
        public async Task AddWhitelistedGuildAsync(string guildId, string guildName)
        {
            var g = await _doraemonContext
                .Set<Guild>()
                .Where(x => x.Id == guildId)
                .SingleOrDefaultAsync();
            if (g is not null)
            {
                throw new ArgumentException("That guild ID is already present on the whitelist.");
            }
            _doraemonContext.Guilds.Add(new Guild { Id = guildId, Name = guildName });
            await _doraemonContext.SaveChangesAsync();
        }
        /// <summary>
        /// Removes a guild from the whitelist, causing all invites from the guild to be moderated by the bot.
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public async Task BlacklistGuildAsync(string guildId)
        {
            var g = await _doraemonContext
                .Set<Guild>()
                .Where(x => x.Id == guildId)
                .SingleOrDefaultAsync();
            if (g is null)
            {
                throw new ArgumentException("That guild ID is not present on the whitelist.");
            }
            _doraemonContext.Guilds.Remove(g);
            await _doraemonContext.SaveChangesAsync();
        }
    }
}
