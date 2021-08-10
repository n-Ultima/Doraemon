using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doraemon.Data.Models.Moderation;
using Doraemon.Common.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class ModmailMessageRepository : RepositoryVersionTwo
    {
        public ModmailMessageRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}
        
        /// <summary>
        /// Creates a new <see cref="ModmailMessage"/> with the specified <see cref="ModmailMessageCreationData"/>.
        /// </summary>
        /// <param name="data">The data needed to construct a new <see cref="ModmailMessage"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the data provided is null.</exception>
        public async Task CreateAsync(ModmailMessageCreationData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.ModmailMessages.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }
        
    }
}