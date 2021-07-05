using Doraemon.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models;

namespace Doraemon.Data.Repositories
{
    public class TagRepository : Repository
    {
        public TagRepository(DoraemonContext doraemonContext)
            :base(doraemonContext)
        { }

        /// <summary>
        /// Creates a new <see cref="Tag"/> with the specified <see cref="TagCreationData"/>
        /// </summary>
        /// <param name="data">The data needed to construc a new <see cref="Tag"/></param>
        /// <returns></returns>
        public async Task CreateAsync(TagCreationData data)
        {
            if(data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            var entity = data.ToEntity();
            await DoraemonContext.Tags.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Fetches a tag.
        /// </summary>
        /// <param name="tagName">The name of the tag to fetch.</param>
        /// <returns>A <see cref="Tag"/> with the given name.</returns>
        public async Task<Tag> FetchAsync(string tagName)
        {
            return await DoraemonContext.Tags
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Updates the response for a given tag.
        /// </summary>
        /// <param name="tagName">The tag's name.</param>
        /// <param name="response">The response to be applied to the tag.</param>
        /// <returns></returns>
        public async Task UpdateResponseAsync(string tagName, string response)
        {
            var tagToEdit = await DoraemonContext.Tags
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tagToEdit is null)
                throw new ArgumentNullException($"The tag provided does not exist.");
            if (tagToEdit.Response == response)
                throw new InvalidOperationException($"That tag already has the given response.");
            tagToEdit.Response = response;
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Transfers ownership of the given tag.
        /// </summary>
        /// <param name="tagName">TThe name of the tag to transfer.</param>
        /// <param name="newOwnerId">The <see cref="ulong"/> ID of the new owner.</param>
        /// <returns></returns>
        public async Task UpdateOwnerAsync(string tagName, ulong newOwnerId)
        {
            var tagToEdit = await DoraemonContext.Tags
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tagToEdit is null)
                throw new ArgumentNullException($"The tag provided does not exist.");
            if (tagToEdit.OwnerId == newOwnerId)
                throw new InvalidOperationException($"The tag provided is already owned by that user.");
            tagToEdit.OwnerId = newOwnerId;
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="tag">The <see cref="Tag"/> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(Tag tag)
        {
            DoraemonContext.Tags.Remove(tag);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}
