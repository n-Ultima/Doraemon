using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models;
using Doraemon.Data;
using Doraemon.Common.Extensions;
using Discord;
using Discord.WebSocket;
using Doraemon.Common;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models.Core;

namespace Doraemon.Data.Services
{
    public class AuthorizationService
    {
        public DiscordSocketClient _client;
        public DoraemonContext _doraemonContext;

        public DoraemonConfiguration DoraemonConfig {get; private set;} = new();
        public AuthorizationService(DiscordSocketClient client, DoraemonContext doraemonContext)
        {
            _client = client;
            _doraemonContext = doraemonContext;
        }
        public async Task<bool> RequireClaims(ulong userId, ClaimMapType claimType)
        {
            var authGuild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var userToAuthenticate = authGuild.GetUser(userId);

            // If they are the guild owner, then they should have every claim. Prevents locks from managing claims.
            if(authGuild.OwnerId == userToAuthenticate.Id)
            {
                return true;
            }
            foreach(var role in userToAuthenticate.Roles.OrderBy(x => x.Position)) // Assuming roles with claims are higher up in the role list, this can save lots of time.
            {
                // Booooooo for circular dependency
                var check = await RoleHasClaimAsync(role.Id, claimType);
                if (check)
                {
                    return true;
                }
            }
            // Even though the return won't get thrown, this prevents whatever is trying to happen to be denied.
            throw new InvalidOperationException($"The following operation could not be authorized: {claimType}");
            return false;
        }
        public async Task<bool> RoleHasClaimAsync(ulong roleId, ClaimMapType claimType)
        {
            var role = await _doraemonContext.ClaimMaps
                .Where(x => x.RoleId == roleId)
                .Where(x => x.Type == claimType)
                .SingleOrDefaultAsync();
            return role is not null;
        }

    }
}
