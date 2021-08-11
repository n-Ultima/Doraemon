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
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class AuthorizationService : DoraemonBotService
    {
        public Snowflake CurrentUser { get; set; }

        public IEnumerable<ClaimMapType> CurrentClaims;
        public AuthorizationService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Requires that the user provided has the claims contain the claim provided.
        /// </summary>
        /// <param name="claimType">The claim to check for.</param>
        
        public void RequireClaims(ClaimMapType claimType)
        {
            RequireAuthenticatedUser();

            var authGuild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var guildMember = authGuild.GetMember(CurrentUser);
            if (guildMember.IsBot) return; // Bots shouldn't be throwing.
            if (CurrentUser == authGuild.OwnerId) return;
            if (CurrentUser == Bot.CurrentUser.Id) return;
            if (CurrentClaims.Contains(claimType)) return;
            throw new Exception($"The following operation could not be authorized. The following claim was missing: {claimType}");
        }

        public async Task AssignCurrentUserAsync(Snowflake userId, IEnumerable<Snowflake> roleIds)
        {
            CurrentUser = userId;
            using (var scope = ServiceProvider.CreateScope())
            {
                var claimMapRepository = scope.ServiceProvider.GetRequiredService<ClaimMapRepository>();
                var currentClaims = await claimMapRepository.RetrievePossessedClaimsAsync(userId, roleIds);
                CurrentClaims = currentClaims;
            }
        }

        private void RequireAuthenticatedUser()
        {
            if (CurrentUser.RawValue == default)
            {
                throw new InvalidOperationException($"There was an error verifying the users' claims.");
            }
        }
    }
}