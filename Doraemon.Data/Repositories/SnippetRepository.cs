using System;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Data.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class SnippetRepository : Repository
    {
        public SnippetRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}
        
        /// <summary>
        /// Creates a new <see cref="ModmailSnippet"/>.
        /// </summary>
        /// <param name="data">The <see cref="ModmailSnippetCreationData"/> needed to construct a new <see cref="ModmailSnippet"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="data"/> provided is null.</exception>
        public async Task CreateAsync(ModmailSnippetCreationData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.ModmailSnippets.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Modifies the snippet provided to hold the new content provided.
        /// </summary>
        /// <param name="snippet">The snippet to modify.</param>
        /// <param name="newContent">The new content that the snippet should hold.</param>
        public async Task ModifyAsync(ModmailSnippet snippet, string newContent)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                snippet.Content = newContent;
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Fetches a snippet with the provided name.
        /// </summary>
        /// <param name="snippetName"></param>
        /// <returns></returns>
        public async Task<ModmailSnippet> FetchAsync(string snippetName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailSnippets
                    .Where(x => x.Name == snippetName)
                    .FirstOrDefaultAsync();
            }
        }

        public async Task DeleteAsync(ModmailSnippet snippet)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.ModmailSnippets.Remove(snippet);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}