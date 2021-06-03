using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Common.Utilities;

namespace Doraemon.Data.Services
{
    public class TagService
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public TagService(DoraemonContext doraemonContext, DiscordSocketClient client)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        /// <summary>
        /// Returns if the tag name provided exists. True if yes, false if no.
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public async Task<bool> TagExistsAsync(string tagName)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == tagName)
                .ToListAsync();
            if (!tag.Any())
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
        public async Task ExecuteTagAsync(string tagName, ulong channel)
        {
            var msgChannel = _client.GetChannel(channel);
            var tag = await _doraemonContext
                .Set<Tag>()
                .FirstOrDefaultAsync(x => x.Name == tagName);
            if (tag is null)
            {
                throw new Exception("The tag provided does not exist.");
            }
            if (!(msgChannel is IMessageChannel messageChannel))
                throw new InvalidOperationException($"The channel provided is not a message channel.");
            await messageChannel.SendMessageAsync(tag.Response);
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
            var id = await DatabaseUtilities.ProduceIdAsync();
            var Tags = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == name.ToLower())
                .ToListAsync();
            if (!Tags.Any())
            {
                _doraemonContext.Tags.Add(new Tag { ownerId = ownerId, Name = name.ToLower(), Response = response, Id = id });
                await _doraemonContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("A tag with that name already exists.");
            }
        }
        /// <summary>
        /// Deletes the tag given by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task DeleteTagAsync(string name)
        {
            var tags = await _doraemonContext
                .Set<Tag>()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
            if (tags is null)
            {
                throw new ArgumentException("That tag was not found.");
            }
            else
            {
                _doraemonContext.Tags.Remove(tags);
                await _doraemonContext.SaveChangesAsync();
            }
        }
        /// <summary>
        /// Takes an already existing tag, and edits that tag's response.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newResponse"></param>
        /// <returns></returns>
        public async Task EditTagResponseAsync(string name, string newResponse)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .FirstOrDefaultAsync(x => x.Name == name);
            if (tag is null)
            {
                throw new ArgumentException("The tag provided was not found.");
            }
            tag.Response = newResponse;
            await _doraemonContext.SaveChangesAsync();
        }
        /// <summary>
        /// Transfers ownership of the tag given to a new user, said user can delete/edit the tag.
        /// </summary>
        /// <param name="tagToTransfer"></param>
        /// <param name="newOwnerId"></param>
        /// <returns></returns>
        public async Task TransferTagOwnershipAsync(string tagToTransfer, ulong newOwnerId)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .FirstOrDefaultAsync(x => x.Name == tagToTransfer);
            if (tag is null)
            {
                throw new ArgumentException("The tag provided was not found.");
            }
            tag.ownerId = newOwnerId;
            await _doraemonContext.SaveChangesAsync();
        }
    }
}
