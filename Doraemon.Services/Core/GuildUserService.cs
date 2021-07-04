using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Services.Core
{
    public class GuildUserService
    {
        public GuildUserRepository _guildUserRepository;
        public GuildUserService(GuildUserRepository guildUserRepository)
        {
            _guildUserRepository = guildUserRepository;
        }

        /// <summary>
        /// Creates a guild user.
        /// </summary>
        /// <param name="userId">The userID of the user.</param>
        /// <param name="username">The username of the user.</param>
        /// <param name="discriminator">The discriminator of the user.</param>
        /// <param name="isModmailBlocked">The <see cref="bool"/> value of if the user can access modmail.</param>
        /// <returns></returns>
        public async Task CreateGuildUserAsync(ulong userId, string username, string discriminator, bool isModmailBlocked)
        {
            await _guildUserRepository.CreateAsync(new GuildUserCreationData()
            {
                Id = userId,
                Username = username,
                Discriminator = discriminator,
                IsModmailBlocked = isModmailBlocked
            });
        }

        /// <summary>
        /// Fetches a guild user.
        /// </summary>
        /// <param name="userId">The userID to query for.</param>
        /// <returns>A <see cref="GuildUser"/>.</returns>
        public async Task<GuildUser> FetchGuildUserAsync(ulong userId)
        {
            return await _guildUserRepository.FetchGuildUserAsync(userId);
        }
#nullable enable
        /// <summary>
        /// Updates the given user with the new properties.
        /// </summary>
        /// <param name="userId">The userID value.</param>
        /// <param name="username">The username to update.</param>
        /// <param name="discriminator">The discriminator to update.</param>
        /// <param name="isModmailBlocked">If the user is or is not modmail blocked.</param>
        /// <returns></returns>
        public async Task UpdateGuildUserAsync(ulong userId, string? username, string? discriminator, bool? isModmailBlocked)
        {
            var userToUpdate = await _guildUserRepository.FetchGuildUserAsync(userId);
            if(userToUpdate is null)
            {
                await _guildUserRepository.CreateAsync(new GuildUserCreationData()
                {
                    Id = userId,
                    Username = username,
                    Discriminator = discriminator,
                    IsModmailBlocked = isModmailBlocked.Value
                });
                return;
            }
            if(username is null 
                && discriminator is null
                && isModmailBlocked is null)
            {
                throw new InvalidOperationException($"The username, discriminator, or modmail-block state must be provided.");
            }
            if(username is not null)
            {
                await _guildUserRepository.UpdateAsync(userToUpdate, username, null, null);
            }
            if(discriminator is not null)
            {
                await _guildUserRepository.UpdateAsync(userToUpdate, null, discriminator, null);
            }
            if(isModmailBlocked is not null)
            {
                await _guildUserRepository.UpdateAsync(userToUpdate, null, null, isModmailBlocked);
            }
        }
    }
#nullable disable
}
