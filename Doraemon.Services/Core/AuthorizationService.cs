using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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

        public ulong CurrentUser { get; set; }

        public IEnumerable<ClaimMapType> CurrentClaims;
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
        
        public async Task RequireClaims(ClaimMapType claimType)
        {
            RequireAuthenticatedUser();
            if (CurrentClaims.Contains(claimType)) return;
            throw new Exception($"The following operation could not be authorized. The following claim was missing: {claimType}");
        }

        public async Task AssignCurrentUserAsync(ulong userId, IEnumerable<ulong> roleIds)
        {
            if(_client.CurrentUser.Id == userId) return;
            CurrentUser = userId;
            var currentClaims = await _claimMapRepository.RetrievePossessedClaimsAsync(userId, roleIds);
            CurrentClaims = currentClaims;
        }

        private void RequireAuthenticatedUser()
        {
            if (CurrentUser == default)
            {
                throw new InvalidOperationException($"There was an error verifying the users' claims.");
            }
        }
    }
}