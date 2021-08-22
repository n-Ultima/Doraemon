using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Common;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Discord.Gateway;
using Remora.Discord.Rest;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class AuthorizationService : DoraemonBotService
    {
        public Snowflake CurrentUser { get; set; }
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestUserAPI _userApi;
        
        public IEnumerable<ClaimMapType> CurrentClaims;
        public AuthorizationService(IServiceProvider serviceProvider, IDiscordRestGuildAPI guildApi, IDiscordRestUserAPI userApi)
            : base(serviceProvider)
        {
            _guildApi = guildApi;
            _userApi = userApi;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Requires that the user provided has the claims contain the claim provided.
        /// </summary>
        /// <param name="claimType">The claim to check for.</param>
        
        public async Task RequireClaims(ClaimMapType claimType)
        {
            RequireAuthenticatedUser();
            var authGuild = await _guildApi.GetGuildAsync(new Snowflake(DoraemonConfig.MainGuildId));
            var guildMember = authGuild.Entity.Members.Value
                .Where(x => x.User.Value.ID == CurrentUser)
                .SingleOrDefault();
            if (guildMember == null)
                throw new Exception($"The user set as the CurrentUser is not currently in the MainGuild provided in config.json");
            if (guildMember.User.Value.IsBot.Value) return; // Bots shouldn't be throwing.
            if (CurrentUser == authGuild.Entity.OwnerID) return;
            var botUser = await _userApi.GetCurrentUserAsync();
            if (CurrentUser == botUser.Entity.ID) return;
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
            if (CurrentUser == default)
            {
                throw new InvalidOperationException($"There was an error verifying the users' claims.");
            }
        }
    }
}