using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class InfractionRepository : RepositoryVersionTwo
    {

        public InfractionRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        ///     Creates a new <see cref="Infraction" /> with the given <see cref="InfractionCreationData" />
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task CreateAsync(InfractionCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var infractionEntity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.Infractions.AddAsync(infractionEntity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Returns an infraction by the specified ID.
        /// </summary>
        /// <param name="caseId">The <see cref="Infraction.Id" /> to search for.</param>
        /// <returns></returns>
        public async Task<Infraction> FetchInfractionByIdAsync(string caseId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                var infractionToRetrieve = await doraemonContext.Infractions
                    .FindAsync(caseId);
                return infractionToRetrieve;   
            }
        }

        /// <summary>
        ///     Fetches all infractions for the given user.
        /// </summary>
        /// <param name="subjectId">The userID to query for.</param>
        /// <returns>A <see cref="List{Infraction}" /></returns>
        public async Task<IEnumerable<Infraction>> FetchAllUserInfractionsAsync(Snowflake subjectId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Infractions
                    .Where(x => x.SubjectId == subjectId)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <summary>
        ///     Updates an infraction's reason.
        /// </summary>
        /// <param name="caseId">The <see cref="Infraction.Id" /> to query for.</param>
        /// <param name="newReason">The new reason to apply to the <see cref="Infraction" />.</param>
        /// <returns></returns>
        public async Task UpdateAsync(string caseId, string newReason)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                var infractionToUpdate = await doraemonContext.Infractions
                    .FindAsync(caseId);
                infractionToUpdate.Reason = newReason;
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Fetches a <see cref="IEnumerable{Infraction}" /> of infractions that have a duration.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Infractions
                    .Where(x => x.Duration != null)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Fetches a list of warns for a user that aren't automoderation.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A <see cref="IEnumerable{Infraction}"/></returns>
        public async Task<IEnumerable<Infraction>> FetchWarnsAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.Infractions
                    .Where(x => x.SubjectId == userId)
                    .Where(x => x.Type == InfractionType.Warn)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        /// <summary>
        ///     Deletes the given infraction.
        /// </summary>
        /// <param name="infraction">The <see cref="Infraction" /> to delete.</param>
        /// <returns></returns>
        public async Task DeleteAsync(Infraction infraction)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                doraemonContext.Infractions.Remove(infraction);
                await doraemonContext.SaveChangesAsync();   
            }
        }
    }
}