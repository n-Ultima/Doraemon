using System.Linq;
using System.Reflection;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services
{
    public static class DoraemonServiceExtension
    {
        /// <summary>
        /// Adds the services marked with the <see cref="DoraemonService"/> attribute as a singleton service.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> that the services will be added to.</param>
        /// <returns></returns>
        public static IServiceCollection AddDoraemonServices(this IServiceCollection serviceCollection)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetCustomAttribute<DoraemonService>() != null && !x.IsAbstract);
            foreach (var type in types)
            {
                serviceCollection.AddSingleton(type);
            }

            return serviceCollection;
        }
    }
}