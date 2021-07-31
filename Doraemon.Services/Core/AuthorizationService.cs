using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class AuthorizationService : DiscordBotService
    {
        private readonly ClaimMapRepository _claimMapRepository;
        public Snowflake CurrentUser { get; set; }

        public IEnumerable<ClaimMapType> CurrentClaims;
        public AuthorizationService(ClaimMapRepository claimMapRepository)
        {
            _claimMapRepository = claimMapRepository;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Requires that the user provided has the claims contain the claim provided.
        /// </summary>
        /// <param name="claimType">The claim to check for.</param>
        
        public void RequireClaims(ClaimMapType claimType)
        {
            var authGuild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            if (CurrentUser == authGuild.OwnerId) return;
            if (CurrentUser == Bot.CurrentUser.Id) return;
            RequireAuthenticatedUser();
            if (CurrentClaims.Contains(claimType)) return;
            throw new Exception($"The following operation could not be authorized. The following claim was missing: {claimType}");
        }

        public async Task AssignCurrentUserAsync(Snowflake userId, IEnumerable<Snowflake> roleIds)
        {
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