using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Services
{
    public class GuildService
    {
        public DoraemonContext _doraemonContext;
        public AuthorizationService _authorizationService;
        public GuildService(DoraemonContext doraemonContext, AuthorizationService authorizationService)
        {
            _doraemonContext = doraemonContext;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Adds a guild to the whitelist, allowing invites to it to remain un-moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="guildName">The name of the guild.</param>
        /// <param name="requestorId">The user requesting that the guild be whitelisted.</param>
        /// <returns></returns>
        public async Task AddWhitelistedGuildAsync(string guildId, string guildName, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
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
        /// Blacklists a guild, causing invites to be moderated.
        /// </summary>
        /// <param name="guildId">The ID of the guild.</param>
        /// <param name="requestorId">The user requesting the blacklist.</param>
        /// <returns></returns>
        public async Task BlacklistGuildAsync(string guildId, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.GuildManage);
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
