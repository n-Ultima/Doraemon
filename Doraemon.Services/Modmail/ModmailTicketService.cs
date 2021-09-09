using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Modmail
{
    [DoraemonService]
    public class ModmailTicketService : DoraemonBotService
    {

        public ModmailTicketService(IServiceProvider serviceProvider)
        : base(serviceProvider)
        {
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                await modmailTicketRepository.CreateAsync(new ModmailTicketCreationData
                {
                    Id = Id,
                    UserId = userId,
                    DmChannelId = dmChannelId,
                    ModmailChannelId = modmailChannelId
                });
            }
        }

        /// <summary>
        /// Returns a modmail ticket with the specified Id."/>
        /// </summary>
        /// <param name="Id">The ID value of the modmail ticket.</param>
        /// <returns>A <see cref="ModmailTicket"/> with the specified Id.</returns>
        public async Task<ModmailTicket> FetchModmailTicketAsync(string Id)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                return await modmailTicketRepository.FetchAsync(Id);

            } 
        }

        /// <summary>
        /// Returns a modmail ticket with the specified user-recipient."/>
        /// </summary>
        /// <param name="userId">The ID value of the user.</param>
        /// <returns>A <see cref="ModmailTicket"/> with the specified user-Id recipient.</returns>
        public async Task<ModmailTicket> FetchModmailTicketAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                return await modmailTicketRepository.FetchAsync(userId);

            }
        }

        public async Task<ModmailTicket> FetchModmailTicketByModmailChannelIdAsync(Snowflake modmailChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                var result = await modmailTicketRepository.FetchByModmailChannelIdAsync(modmailChannelId);
                return result;
            }
        }

        public async Task<ModmailTicket> FetchModmailTicketByDmChannelIdAsync(Snowflake dmChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                return await modmailTicketRepository.FetchByDmChannelIdAsync(dmChannelId);

            }
        }

        public async Task AddMessageToModmailTicketAsync(string ticketId, Snowflake authorId, Snowflake messageId, string content)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailMessageRepository = scope.ServiceProvider.GetRequiredService<ModmailMessageRepository>();
                await modmailMessageRepository.CreateAsync(new ModmailMessageCreationData()
                {
                    TicketId = ticketId,
                    AuthorId = authorId,
                    Content = content,
                    MessageId = messageId
                });
            }
        }

        public async Task<IEnumerable<ModmailMessage>> FetchModmailMessagesAsync(string ticketId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                return await modmailTicketRepository.FetchModmailMessagesAsync(ticketId);
                
            }
        }

        public async Task DeleteModmailTicketAsync(string Id)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailTicketRepository = scope.ServiceProvider.GetRequiredService<ModmailTicketRepository>();
                await modmailTicketRepository.DeleteAsync(Id);
            }
        }
    }
}