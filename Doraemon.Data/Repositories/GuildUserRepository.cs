using System;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class GuildUserRepository : Repository
    {
        private readonly IDbContextFactory<DoraemonContext> dbContextFactory;
        public GuildUserRepository(DoraemonContext doraemonContext, IDbContextFactory<DoraemonContext> dbContextFactory)
            : base(doraemonContext)
        {
            this.dbContextFactory = dbContextFactory;
        }
        private static readonly RepositoryTransactionFactory _createTransactionFactory = new RepositoryTransactionFactory();
        public Task<IRepositoryTransaction> BeginCreateTransactionAsync()
            => _createTransactionFactory.BeginTransactionAsync(DoraemonContext.Database);
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
            await DoraemonContext.GuildUsers.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();

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
            if (username is not null)
            {
                user.Username = username;
                await DoraemonContext.SaveChangesAsync();
            }

            if (discriminator is not null)
            {
                user.Discriminator = discriminator;
                await DoraemonContext.SaveChangesAsync();
            }

            if (isModmailBlocked is not null)
            {
                user.IsModmailBlocked = isModmailBlocked.Value;
                await DoraemonContext.SaveChangesAsync();
            }
        }
#nullable disable

        /// <summary>
        ///     Fetches a guild user.
        /// </summary>
        /// <param name="userId">The ID of the guild user to query for.</param>
        /// <returns>A <see cref="GuildUser" /> with the specified ID.</returns>
        public async Task<GuildUser> FetchGuildUserAsync(ulong userId)
        {
            return await DoraemonContext.GuildUsers
                .FindAsync(userId);
        }
    }
}