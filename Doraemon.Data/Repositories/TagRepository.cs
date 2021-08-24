using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class TagRepository : Repository
    {
        public TagRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        ///     Creates a new <see cref="Tag" /> with the specified <see cref="TagCreationData" />
        /// </summary>
        /// <param name="data">The data needed to construc a new <see cref="Tag" /></param>
        /// <returns></returns>
        public async Task CreateAsync(TagCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.Tags.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Fetches a tag.
        /// </summary>
        /// <param name="tagName">The name of the tag to fetch.</param>
        /// <returns>A <see cref="Tag" /> with the given name.</returns>
        public async Task<Tag> FetchAsync(string tagName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Tags
                    .Where(x => x.Name == tagName)
                    .SingleOrDefaultAsync();
            }
        }

        /// <summary>
        ///     Updates the response for a given tag.
        /// </summary>
        /// <param name="tagName">The tag's name.</param>
        /// <param name="response">The response to be applied to the tag.</param>
        /// <returns></returns>
        public async Task UpdateResponseAsync(string tagName, string response)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                var tagToEdit = await doraemonContext.Tags
                    .Where(x => x.Name == tagName)
                    .SingleOrDefaultAsync();
                if (tagToEdit is null)
                    throw new ArgumentException("The tag provided does not exist.");
                if (tagToEdit.Response == response)
                    throw new InvalidOperationException("That tag already has the given response.");
                tagToEdit.Response = response;
                await doraemonContext.SaveChangesAsync();
            }

        }

        /// <summary>
        ///     Transfers ownership of the given tag.
        /// </summary>
        /// <param name="tagName">TThe name of the tag to transfer.</param>
        /// <param name="newOwnerId">The <see cref="ulong" /> ID of the new owner.</param>
        /// <returns></returns>
        public async Task UpdateOwnerAsync(string tagName, ulong newOwnerId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                var tagToEdit = await doraemonContext.Tags
                    .Where(x => x.Name == tagName)
                    .SingleOrDefaultAsync();
                if (tagToEdit is null)
                    throw new ArgumentException("The tag provided does not exist.");
                if (tagToEdit.OwnerId == newOwnerId)
                    throw new InvalidOperationException("The tag provided is already owned by that user.");
                tagToEdit.OwnerId = newOwnerId;
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Fetches a list of tags from the database.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Tag}"/> which contains every tag present.</returns>
        public async Task<IEnumerable<Tag>> FetchAllTagsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Tags
                    .AsNoTracking()
                    .AsQueryable()
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
        }
        
        /// <summary>
        ///     Deletes a tag.
        /// </summary>
        /// <param name="tag">The <see cref="Tag" /> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(Tag tag)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.Tags.Remove(tag);
                await doraemonContext.SaveChangesAsync();   
            }
        }
    }
}