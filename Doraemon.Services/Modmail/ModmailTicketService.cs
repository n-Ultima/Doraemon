using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Modmail
{
    [DoraemonService]
    public class ModmailTicketService
    {
        private readonly ModmailTicketRepository _modmailTicketRepository;
        private readonly ModmailMessageRepository _modmailMessageRepository;

        public ModmailTicketService(ModmailTicketRepository modmailTicketRepository, ModmailMessageRepository modmailMessageRepository)
        {
            _modmailTicketRepository = modmailTicketRepository;
            _modmailMessageRepository = modmailMessageRepository;
        }

        /// <summary>
        /// Creates a modmail ticket.
        /// </summary>
        /// <param name="Id">The ID value of this modmail ticket.</param>
        /// <param name="userId">The user who started the thread.</param>
        /// <param name="dmChannelId">The DM Channel ID of the recipient.</param>
        /// <param name="modmailChannelId">The modmail channel ID inside of the guild.</param>
        public async Task CreateModmailTicketAsync(string Id, Snowflake userId, Snowflake dmChannelId, Snowflake modmailChannelId)
        {
            using (var transaction = await _modmailTicketRepository.BeginCreateTransactionAsync())
            {
                await _modmailTicketRepository.CreateAsync(new ModmailTicketCreationData
                {
                    Id = Id,
                    UserId = userId,
                    DmChannelId = dmChannelId,
                    ModmailChannelId = modmailChannelId
                });   
                transaction.Commit();
            }
        }

        /// <summary>
        /// Returns a modmail ticket with the specified Id."/>
        /// </summary>
        /// <param name="Id">The ID value of the modmail ticket.</param>
        /// <returns>A <see cref="ModmailTicket"/> with the specified Id.</returns>
        public async Task<ModmailTicket> FetchModmailTicketAsync(string Id)
        {
            return await _modmailTicketRepository.FetchAsync(Id);
        }
        /// <summary>
        /// Returns a modmail ticket with the specified user-recipient."/>
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <returns>A <see cref="ModmailTicket"/> with the specified user-Id recipient.</returns>
        public async Task<ModmailTicket> FetchModmailTicketAsync(Snowflake userId)
        {
            return await _modmailTicketRepository.FetchAsync(userId);
        }

        // Somehow, some way, this shit throws parallelism. Fuck off.
        public async Task<ModmailTicket> FetchModmailTicketByModmailChannelIdAsync(Snowflake modmailChannelId)
        {
            using (var transactinon = await _modmailTicketRepository.BeginCreateTransactionAsync())
            { 
                var result = await _modmailTicketRepository.FetchByModmailChannelIdAsync(modmailChannelId);
                transactinon.Commit();
                return result;
            }
        }

        public async Task<ModmailTicket> FetchModmailTicketByDmChannelIdAsync(Snowflake dmChannelId)
        {
            return await _modmailTicketRepository.FetchByDmChannelIdAsync(dmChannelId);
        }

        public async Task AddMessageToModmailTicketAsync(string ticketId, Snowflake authorId, string content)
        {
            using (var transaction = await _modmailMessageRepository.BeginCreateTransactionAsync())
            {
                await _modmailMessageRepository.CreateAsync(new ModmailMessageCreationData()
                {
                    TicketId = ticketId,
                    AuthorId = authorId,
                    Content = content
                });
                transaction.Commit();
            }
        }

        public async Task<IEnumerable<ModmailMessage>> FetchModmailMessagesAsync(string ticketId)
        {
            return await _modmailTicketRepository.FetchModmailMessagesAsync(ticketId);
        }
        public async Task DeleteModmailTicketAsync(string Id)
        {
            await _modmailTicketRepository.DeleteAsync(Id);
        }
    }
}