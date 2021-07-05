using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data;
using Discord.WebSocket;
using Doraemon.Services.Core;
using Doraemon.Data.Models.Core;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Data.Models;
using Discord;
using Doraemon.Common.Utilities;
using Doraemon.Data.Repositories;

namespace Doraemon.Services.PromotionServices
{
    public class TagService
    {
        public DiscordSocketClient _client;
        private readonly TagRepository _tagRepository;
        public AuthorizationService _authorizationService;
        public TagService(DiscordSocketClient client, AuthorizationService authorizationService, TagRepository tagRepository)
        {
            _client = client;
            _authorizationService = authorizationService;
            _tagRepository = tagRepository;
        }
        /// <summary>
        /// Returns if the tag name provided exists. True if yes, false if no.
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public async Task<bool> TagExistsAsync(string tagName)
        {
            var tag = await _tagRepository.FetchAsync(tagName);
            if(tag is null)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Executes the tag given, and sends it to the channelID given.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ExecuteTagAsync(string tagName, ulong channel, MessageReference reference = null)
        {
            var msgChannel = _client.GetChannel(channel);
            var tag = await _tagRepository.FetchAsync(tagName);
            if (!(msgChannel is IMessageChannel messageChannel))
                throw new InvalidOperationException($"The channel provided is not a message channel.");
            if (tag is null)
            {
                return;
            }
            if (reference == null)
            {
                await messageChannel.SendMessageAsync(tag.Response);
            }
            else
            {
                await messageChannel.SendMessageAsync(tag.Response, messageReference: reference);
            }
        }
        /// <summary>
        /// Creates a tag with the given response.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ownerId"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task CreateTagAsync(string name, ulong ownerId, string response)
        {
            await _authorizationService.RequireClaims(ownerId, ClaimMapType.TagManage);
            var id = DatabaseUtilities.ProduceId();
            var tag = await _tagRepository.FetchAsync(name);
            if (tag is not null)
            {
                throw new InvalidOperationException("A tag with that name already exists.");
            }
            await _tagRepository.CreateAsync(new TagCreationData()
            {
                Id = id,
                OwnerId = ownerId,
                Name = name,
                Response = response
            });
        }
        /// <summary>
        /// Deletes the tag given by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task DeleteTagAsync(string name, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.TagManage);
            var tags = await _tagRepository.FetchAsync(name);
            if (tags is null)
            {
                throw new ArgumentException("That tag was not found.");
            }
            else
            {
                await _tagRepository.DeleteAsync(tags);
            }
        }
        /// <summary>
        /// Takes an already existing tag, and edits that tag's response.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newResponse"></param>
        /// <returns></returns>
        public async Task EditTagResponseAsync(string name, string newResponse, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.TagManage);
            var tag = await _tagRepository.FetchAsync(name);
            if (tag is null)
            {
                throw new ArgumentException("The tag provided was not found.");
            }
            await _tagRepository.UpdateResponseAsync(name, newResponse);
        }
        /// <summary>
        /// Transfers ownership of the tag given to a new user, said user can delete/edit the tag.
        /// </summary>
        /// <param name="tagToTransfer"></param>
        /// <param name="newOwnerId"></param>
        /// <returns></returns>
        public async Task TransferTagOwnershipAsync(string tagToTransfer, ulong newOwnerId, ulong requestorId)
        {
            await _authorizationService.RequireClaims(requestorId, ClaimMapType.TagManage);
            var tag = await _tagRepository.FetchAsync(tagToTransfer);
            if (tag is null)
            {
                throw new ArgumentNullException("The tag provided was not found.");
            }
            await _tagRepository.UpdateOwnerAsync(tag.Name, newOwnerId);
        }
    }
}
