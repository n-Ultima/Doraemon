using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.Moderation
{
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

        public async Task<ModmailTicket> FetchModmailTicketAsync(string Id)
        {
            return await _modmailTicketRepository.FetchAsync(Id);
        }

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