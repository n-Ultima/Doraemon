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

namespace Doraemon.Data.Services
{
    public class AuthorizationService
    {
        public DiscordSocketClient _client;
        public RoleClaimService _roleClaimService;

        public DoraemonConfiguration DoraemonConfig {get; private set;} = new();
        public AuthorizationService(DiscordSocketClient client, RoleClaimService roleClaimService)
        {
            _client = client;
            _roleClaimService = roleClaimService;
        }
        public async Task<bool> RequireClaims(ulong userId, ClaimMapType claimType)
        {
            var authGuild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var userToAuthenticate = authGuild.GetUser(userId);

            foreach(var role in userToAuthenticate.Roles.OrderBy(x => x.Position)) // Assuming roles with claims are higher up in the role list, this can save lots of time.
            {
                var check = await _roleClaimService.RoleHasClaimAsync(role.Id, claimType);
                if (check)
                {
                    return true;
                }
            }
            throw new InvalidOperationException($"The following operation could not be authorized: {claimType}"); // Even though the return won't get thrown, this prevents whatever is trying to happen to be denied.
            return false;
        }
    }
}
