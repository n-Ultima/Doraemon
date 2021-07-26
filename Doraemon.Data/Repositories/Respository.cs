using System.Threading.Tasks;

namespace Doraemon.Data.Repositories
{
    /// <summary>
    ///     Handles all data storage and retrieval operations.
    /// </summary>
    public abstract class Repository
    {
        /// <summary>
        ///     Creates a new <see cref="Repository" />
        /// </summary>
        /// <param name="doraemonContext">The value used for the <see cref="DoraemonContext" />.</param>
        public Repository(DoraemonContext doraemonContext)
        {
            DoraemonContext = doraemonContext;
        }
        
        internal protected DoraemonContext DoraemonContext { get; }
    }
}