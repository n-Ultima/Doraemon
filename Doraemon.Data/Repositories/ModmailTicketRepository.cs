using Doraemon.Data.Models.Moderation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
namespace Doraemon.Data.Repositories
{
    public class ModmailTicketRepository : Repository
    {
        public ModmailTicketRepository(DoraemonContext doraemonContext)
            : base(doraemonContext)
        { }

        /// <summary>
        /// Creates a <see cref="ModmailTicket"/> with the specified <see cref="ModmailTicketCreationData"/>
        /// </summary>
        /// <param name="data">The data needed to construct a new <see cref="ModmailTicket"/></param>
        /// <returns></returns>
        public async Task CreateAsync(ModmailTicketCreationData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            var entity = data.ToEntity();
            await DoraemonContext.ModmailTickets.AddAsync(entity);
            await DoraemonContext.SaveChangesAsync();
        }

        /// <summary>
        /// Fetches a modmail ticket.
        /// </summary>
        /// <param name="userId">The userId that started the ticket.</param>
        /// <returns>A <see cref="ModmailTicket"/></returns>
        public async Task<ModmailTicket> FetchAsync(ulong userId)
        {
            return await DoraemonContext.ModmailTickets.Where(x => x.UserId == userId).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Fetches a modmail ticket.
        /// </summary>
        /// <param name="Id">The <see cref="Guid"/> of the ticket.</param>
        /// <returns>A <see cref="ModmailTicket"/></returns>
        public async Task<ModmailTicket> FetchAsync(string Id)
        {
            return await DoraemonContext.ModmailTickets.FindAsync(Id);
        }

        /// <summary>
        /// Fetches a modmail ticket by the modmail channel ID.
        /// </summary>
        /// <param name="modmailChannelId">The <see cref="ModmailTicket.ModmailChannelid"/></param>
        /// <returns>A <see cref="ModmailTicket"/></returns>
        public async Task<ModmailTicket> FetchByModmailChannelIdAsync(ulong modmailChannelId)
        {
            return await DoraemonContext.ModmailTickets.Where(x => x.ModmailChannelId == modmailChannelId).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Fetches a modmail ticket by the DmChannel Id.
        /// </summary>
        /// <param name="dmChannelId">The <see cref="ModmailTicket.DmChannelId"/></param>
        /// <returns>A <see cref="ModmailTicket"/></returns>
        public async Task<ModmailTicket> FetchByDmChannelIdAsync(ulong dmChannelId)
        {
            return await DoraemonContext.ModmailTickets.Where(x => x.DmChannelId == dmChannelId).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Deltes a modmail ticket.
        /// </summary>
        /// <param name="Id">The <see cref="ModmailTicket.Id"/></param>
        /// <returns></returns>
        public async Task DeleteAsync(string Id)
        {
            var ticket = await DoraemonContext.ModmailTickets
                .FindAsync(Id);
            if(ticket is null)
            {
                throw new InvalidOperationException($"The ticket ID provided doesn't exist.");
            }
            DoraemonContext.ModmailTickets.Remove(ticket);
            await DoraemonContext.SaveChangesAsync();
        }
    }
}
