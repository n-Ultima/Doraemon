using System;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class GuildUserRepository : RepositoryVersionTwo
    {
        public GuildUserRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        ///     Creates a new <see cref="GuildUser" /> with the specified <see cref="GuildUserCreationData" />
        /// </summary>
        /// <param name="data">The data need to construct a new <see cref="GuildUser" /></param>
        /// <returns></returns>
        /// We have to create a new context here using a <see cref="IDbContextFactory{TContext}"/> due to a likely 
        public async Task CreateAsync(GuildUserCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.GuildUsers.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }

        }

#nullable enable
        /// <summary>
        ///     Updates an existing guild user.
        /// </summary>
        /// <param name="user">The <see cref="GuildUser" /> to update.</param>
        /// <param name="username">The <see cref="GuildUser.Username" /> to apply.</param>
        /// <param name="discriminator">The <see cref="GuildUser.Discriminator" /> to apply.</param>
        /// <param name="isModmailBlocked">The <see cref="GuildUser.IsModmailBlocked" /> to apply.</param>
        /// <returns></returns>
        public async Task UpdateAsync(GuildUser user, string? username, string? discriminator, bool? isModmailBlocked)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                if (username is not null)
                {
                    user.Username = username;
                    await doraemonContext.SaveChangesAsync();
                }

                if (discriminator is not null)
                {
                    user.Discriminator = discriminator;
                    await doraemonContext.SaveChangesAsync();
                }

                if (isModmailBlocked is not null)
                {
                    user.IsModmailBlocked = isModmailBlocked.Value;
                    await doraemonContext.SaveChangesAsync();
                }
            }
        }
#nullable disable

        /// <summary>
        ///     Fetches a guild user.
        /// </summary>
        /// <param name="userId">The ID of the guild user to query for.</param>
        /// <returns>A <see cref="GuildUser" /> with the specified ID.</returns>
        public async Task<GuildUser> FetchGuildUserAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.GuildUsers
                    .FindAsync(userId);
            }
        }
    }
}