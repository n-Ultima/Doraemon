using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class AuthorizationService
    {
        private readonly ClaimMapRepository _claimMapRepository;
        private readonly DiscordSocketClient _client;

        public AuthorizationService(DiscordSocketClient client, ClaimMapRepository claimMapRepository)
        {
            _client = client;
            _claimMapRepository = claimMapRepository;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Requires that the user provided has the claims contain the claim provided.
        /// </summary>
        /// <param name="userId">The user ID that claims should be checked against.</param>
        /// <param name="claimType">The claim to check for.</param>
        
        public async Task RequireClaims(ulong userId, ClaimMapType claimType)
        {
            if (userId == _client.CurrentUser.Id) return;
            var authGuild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var userToAuthenticate = authGuild.GetUser(userId);

            if (authGuild.OwnerId == userToAuthenticate.Id) return;

            var currentClaims = await _claimMapRepository.FetchAllClaimsForUserAsync(userId);
            if (currentClaims.Contains(claimType)) return;
            if (userToAuthenticate is null)
                throw new Exception($"The user attempting to be authenticated is not present in the guild.");
            
            throw new Exception($"The operation could not be authorized. The following claims were missing: {claimType}");

        }
    }
}