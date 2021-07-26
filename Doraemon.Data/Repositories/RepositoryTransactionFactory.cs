using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Doraemon.Data.Repositories
{
    #nullable  enable
    #pragma warning disable EF1001
    public class RepositoryTransactionFactory
    {
        public async Task<IRepositoryTransaction> BeginTransactionAsync(DatabaseFacade database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            return new RepositoryTransaction(
                (database.CurrentTransaction is null)
                    ? await database.BeginTransactionAsync()
                    : null,
                await _lockProvider.LockAsync());
        }
        public async Task<IRepositoryTransaction> BeginTransactionAsync(DatabaseFacade database, CancellationToken cancelToken)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            var databaseTransaction = (database.CurrentTransaction == null)
                ? await database.BeginTransactionAsync(cancelToken)
                : null;
            var asyncLock = await _lockProvider.LockAsync(cancelToken);

            return new RepositoryTransaction(
                databaseTransaction,
                asyncLock);
        }
        private AsyncLock _lockProvider { get; }
            = new AsyncLock();
        private class RepositoryTransaction : IRepositoryTransaction
        {
            public RepositoryTransaction(IDbContextTransaction? transaction, IDisposable @lock)
            {
                _transaction = transaction;
                _lock = @lock;
            }

            public void Commit()
            {
                if (!_hasCommitted)
                {
                    _transaction?.Commit();
                    _hasCommitted = true;
                }
            }

            public void Dispose()
            {
                if(!_hasDisposed)
                {
                    if (!_hasCommitted)
                        _transaction?.Rollback();

                    _lock.Dispose();

                    _hasDisposed = true;
                }
            }

            private bool _hasCommitted
                = false;

            private bool _hasDisposed
                = false;

            private readonly IDbContextTransaction? _transaction;

            private readonly IDisposable _lock;
        }
        #nullable  disable
    #pragma warning restore EF1001

    }
}