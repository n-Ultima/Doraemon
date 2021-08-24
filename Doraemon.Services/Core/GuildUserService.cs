using System;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Core
{
    [DoraemonService]
    public class GuildUserService : DoraemonBotService
    {

        private readonly AuthorizationService _authorizationService;
        public GuildUserService(AuthorizationService authorizationService, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _authorizationService = authorizationService;
        }

        /// <summary>
        ///     Creates a guild user.
        /// </summary>
        /// <param name="userId">The userID of the user.</param>
        /// <param name="username">The username of the user.</param>
        /// <param name="discriminator">The discriminator of the user.</param>
        /// <param name="isModmailBlocked">The <see cref="bool" /> value of if the user can access modmail.</param>
        /// <returns></returns>
        public async Task CreateGuildUserAsync(Snowflake userId, string username, string discriminator,
            bool isModmailBlocked)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildUserRepository = scope.ServiceProvider.GetRequiredService<GuildUserRepository>();
                await guildUserRepository.CreateAsync(new GuildUserCreationData
                {
                    Id = userId,
                    Username = username,
                    Discriminator = discriminator,
                    IsModmailBlocked = isModmailBlocked
                });
            }
        }

        /// <summary>
        ///     Fetches a guild user.
        /// </summary>
        /// <param name="userId">The userID to query for.</param>
        /// <returns>A <see cref="GuildUser" />.</returns>
        public async Task<GuildUser> FetchGuildUserAsync(ulong userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildUserRepository = scope.ServiceProvider.GetRequiredService<GuildUserRepository>();
                return await guildUserRepository.FetchGuildUserAsync(userId);
            }
        }
#nullable enable
# pragma warning disable 8629
        /// <summary>
        ///     Updates the given user with the new properties.
        /// </summary>
        /// <param name="userId">The userID value.</param>
        /// <param name="username">The username to update.</param>
        /// <param name="discriminator">The discriminator to update.</param>
        /// <param name="isModmailBlocked">If the user is or is not modmail blocked.</param>
        /// <returns></returns>
        public async Task UpdateGuildUserAsync(Snowflake userId, string? username, string? discriminator,
            bool? isModmailBlocked)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var guildUserRepository = scope.ServiceProvider.GetRequiredService<GuildUserRepository>();
                var userToUpdate = await guildUserRepository.FetchGuildUserAsync(userId);
                if (userToUpdate is null)
                {
                    await guildUserRepository.CreateAsync(new GuildUserCreationData
                    {
                        Id = userId,
                        Username = username,
                        Discriminator = discriminator,
                        IsModmailBlocked = isModmailBlocked.Value
                    });
                    return;
                }

                if (username is null
                    && discriminator is null
                    && isModmailBlocked is null)
                    throw new ArgumentException(
                        "The username, discriminator, or modmail-block state must be provided.");
                if (username is not null)
                    await guildUserRepository.UpdateAsync(userToUpdate, username, null, null);
                if (discriminator is not null)
                    await guildUserRepository.UpdateAsync(userToUpdate, null, discriminator, null);
                if (isModmailBlocked is not null)
                {
                    _authorizationService.RequireClaims(ClaimMapType.ModmailBlock);
                    await guildUserRepository.UpdateAsync(userToUpdate, null, null, isModmailBlocked);
                }
            }
        }
    }
#nullable disable
# pragma warning restore 8629
}