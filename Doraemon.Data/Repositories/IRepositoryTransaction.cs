using System;

namespace Doraemon.Data.Repositories
{
    public interface IRepositoryTransaction : IDisposable
    {
        /// <summary>
        /// Commits any changes performed during the transaction the database.
        /// </summary>
        void Commit();
    }
}