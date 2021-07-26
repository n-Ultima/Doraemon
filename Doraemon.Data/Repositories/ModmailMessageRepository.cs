using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Data.Models.Moderation;
using Doraemon.Common.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class ModmailMessageRepository : Repository
    {
        public ModmailMessageRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        {}
        private static readonly RepositoryTransactionFactory _createTransactionFactory = new RepositoryTransactionFactory();
        public Task<IRepositoryTransaction> BeginCreateTransactionAsync()
            => _createTransactionFactory.BeginTransactionAsync(DoraemonContext.Database);
        public async Task CreateAsync(ModmailMessageCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            await DoraemonContext.ModmailMessages.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }
        
    }
}