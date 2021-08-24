using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Data.Repositories
{
    [DoraemonRepository]
    public class ModmailTicketRepository : Repository
    {
        public ModmailTicketRepository(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        /// <summary>
        ///     Creates a <see cref="ModmailTicket" /> with the specified <see cref="ModmailTicketCreationData" />
        /// </summary>
        /// <param name="data">The data needed to construct a new <see cref="ModmailTicket" /></param>
        /// <returns></returns>
        public async Task CreateAsync(ModmailTicketCreationData data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            var entity = data.ToEntity();
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                await doraemonContext.ModmailTickets.AddAsync(entity);
                await doraemonContext.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Fetches a modmail ticket.
        /// </summary>
        /// <param name="userId">The userId that started the ticket.</param>
        /// <returns>A <see cref="ModmailTicket" /></returns>
        public async Task<ModmailTicket> FetchAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailTickets
                    .Where(x => x.UserId == userId)
                    .AsNoTracking().
                    SingleOrDefaultAsync();
            }
        }

        /// <summary>
        ///     Fetches a modmail ticket.
        /// </summary>
        /// <param name="Id">The <see cref="Guid" /> of the ticket.</param>
        /// <returns>A <see cref="ModmailTicket" /></returns>
        public async Task<ModmailTicket> FetchAsync(string Id)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailTickets
                    .FindAsync(Id);
            }
        }

        /// <summary>
        ///     Fetches a modmail ticket by the modmail channel ID.
        /// </summary>
        /// <param name="modmailChannelId">The <see cref="ModmailTicket.ModmailChannelid" /></param>
        /// <returns>A <see cref="ModmailTicket" /></returns>
        public async Task<ModmailTicket> FetchByModmailChannelIdAsync(ulong modmailChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailTickets
                    .Where(x => x.ModmailChannelId == modmailChannelId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();   
            }
        }

        /// <summary>
        ///     Fetches a modmail ticket by the DmChannel Id.
        /// </summary>
        /// <param name="dmChannelId">The <see cref="ModmailTicket.DmChannelId" /></param>
        /// <returns>A <see cref="ModmailTicket" /></returns>
        public async Task<ModmailTicket> FetchByDmChannelIdAsync(Snowflake dmChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailTickets
                    .Where(x => x.DmChannelId == dmChannelId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
            }
        }

        public async Task<IEnumerable<ModmailMessage>> FetchModmailMessagesAsync(string ticketId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                return await doraemonContext.ModmailMessages
                    .Where(x => x.TicketId == ticketId)
                    .AsNoTracking()
                    .ToListAsync();
                
            }
        }
        /// <summary>
        ///     Deletes a modmail ticket and all messages inside it.
        /// </summary>
        /// <param name="Id">The <see cref="ModmailTicket.Id" /></param>
        /// <returns></returns>
        public async Task DeleteAsync(string Id)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                var ticket = await doraemonContext.ModmailTickets
                    .FindAsync(Id);
                var messages = await doraemonContext.ModmailMessages
                    .Where(x => x.TicketId == Id)
                    .ToListAsync();
                if (ticket is null) throw new InvalidOperationException("The ticket ID provided doesn't exist.");
                doraemonContext.ModmailTickets.Remove(ticket);
                doraemonContext.ModmailMessages.RemoveRange(messages);
                await doraemonContext.SaveChangesAsync();
            }
        }
    }
}