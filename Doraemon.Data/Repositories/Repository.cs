using System;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    /// <summary>
    /// Initialized a new instance of this repository, used to handle all manipulations and data retrieval from our database.
    /// </summary>
    public abstract class Repository
    {
        
        /// <summary>
        /// The <see cref="IServiceProvider"/> used to create <see cref="IServiceScope"/>s, which can be used to access our <see cref="DoraemonContext"/>.
        /// </summary>
        internal protected IServiceProvider ServiceProvider;

        public Repository(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}