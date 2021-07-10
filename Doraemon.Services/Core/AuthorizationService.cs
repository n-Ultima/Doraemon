using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    public class AuthorizationService
    {
        public ClaimMapRepository _claimMapRepository;
        public DiscordSocketClient _client;
        public DoraemonContext _doraemonContext;

        public AuthorizationService(DiscordSocketClient client, DoraemonContext doraemonContext,
            ClaimMapRepository claimMapRepository)
        {
            _client = client;
            _doraemonContext = doraemonContext;
            _claimMapRepository = claimMapRepository;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Used in scoped services to authorize actions.
        /// </summary>
        /// <param name="userId">The user ID that claims should be checked against.</param>
        /// <param name="claimType">The claim to check for.</param>
        /// <returns>A <see cref="bool" /> depending on if the user has the claim.</returns>
        public async Task<bool> RequireClaims(ulong userId, ClaimMapType claimType)
        {
            var authGuild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var userToAuthenticate = authGuild.GetUser(userId);
            if (userToAuthenticate is null) return false;
            // If they are the guild owner, then they should have every claim. Prevents locks from managing claims.
            if (authGuild.OwnerId == userToAuthenticate.Id) return true;
            // User claims override role claims.
            
            foreach (var role in
                userToAuthenticate.Roles.OrderBy(x =>
                    x.Position)) // Assuming roles with claims are higher up in the role list, this can save lots of time.
            {
                var check = await _claimMapRepository.FetchSingleRoleClaimAsync(role.Id, claimType);
                if (check is not null) return true;
            }
            if (await _claimMapRepository.FetchSingleUserClaimAsync(userId, claimType) is null)
            {
                throw new InvalidOperationException($"The following operation could not be authorized: {claimType}");
            }
            return true;
        }
    }
}