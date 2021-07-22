using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    public static class DoraemonRepositoryServiceExtension
    {
        /// <summary>
        /// Adds scoped services to the <see cref="IServiceCollection"/> as a scoped service, marked with the <see cref="DoraemonRepository"/>
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the scoped services to.</param>
        /// <returns></returns>
        public static IServiceCollection AddDoraemonRepositories(this IServiceCollection serviceCollection)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetCustomAttribute<DoraemonRepository>() != null && !x.IsAbstract);
            foreach (var type in types)
            {
                serviceCollection.AddScoped(type);
            }

            return serviceCollection;
        }
    }
}