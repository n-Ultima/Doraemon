using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Moderation
{
    [DoraemonService]
    public class ModmailTicketService
    {
        private readonly DiscordSocketClient _client;
        private readonly ModmailTicketRepository _modmailTicketRepository;
        private readonly ModmailMessageRepository _modmailMessageRepository;

        public ModmailTicketService(ModmailTicketRepository modmailTicketRepository, DiscordSocketClient client, ModmailMessageRepository modmailMessageRepository)
        {
            _modmailTicketRepository = modmailTicketRepository;
            _client = client;
            _modmailMessageRepository = modmailMessageRepository;
        }

        /// <summary>
        /// Creates a modmail ticket.
        /// </summary>
        /// <param name="Id">The ID value of this modmail ticket.</param>
        /// <param name="userId">The user who started the thread.</param>
        /// <param name="dmChannelId">The DM Channel ID of the recipient.</param>
        /// <param name="modmailChannelId">The modmail channel ID inside of the guild.</param>
        public async Task CreateModmailTicketAsync(string Id, ulong userId, ulong dmChannelId, ulong modmailChannelId)
        {
            await _modmailTicketRepository.CreateAsync(new ModmailTicketCreationData
            {
                Id = Id,
                UserId = userId,
                DmChannelId = dmChannelId,
                ModmailChannelId = modmailChannelId
            });
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
        public async Task<ModmailTicket> FetchModmailTicketAsync(ulong userId)
        {
            return await _modmailTicketRepository.FetchAsync(userId);
        }

        public async Task<ModmailTicket> FetchModmailTicketByModmailChannelIdAsync(ulong modmailChannelId)
        {
            return await _modmailTicketRepository.FetchByModmailChannelIdAsync(modmailChannelId);
        }

        public async Task<ModmailTicket> FetchModmailTicketByDmChannelIdAsync(ulong dmChannelId)
        {
            return await _modmailTicketRepository.FetchByDmChannelIdAsync(dmChannelId);
        }

        public async Task AddMessageToModmailTicketAsync(string ticketId, ulong authorId, string content)
        {
            await _modmailMessageRepository.CreateAsync(new ModmailMessageCreationData()
            {
                TicketId = ticketId,
                AuthorId = authorId,
                Content = content
            });
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