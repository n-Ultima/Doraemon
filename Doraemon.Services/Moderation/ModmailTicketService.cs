﻿using Discord.WebSocket;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Services.Moderation
{
    public class ModmailTicketService
    {
        private readonly ModmailTicketRepository _modmailTicketRepository;
        private readonly DiscordSocketClient _client;

        public ModmailTicketService(ModmailTicketRepository modmailTicketRepository, DiscordSocketClient client)
        {
            _modmailTicketRepository = modmailTicketRepository;
            _client = client;
        }
        public async Task CreateModmailTicketAsync(string Id, ulong userId, ulong dmChannelId, ulong modmailChannelId)
        {
            await _modmailTicketRepository.CreateAsync(new ModmailTicketCreationData()
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

        public async Task DeleteModmailTicketAsync(string Id)
        {
            await _modmailTicketRepository.DeleteAsync(Id);
        }
    }
}