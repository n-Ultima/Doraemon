using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Utilities;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;

namespace Doraemon.Services.PromotionServices
{
    [DoraemonService]
    public class TagService : DiscordBotService
    {
        private readonly TagRepository _tagRepository;
        private readonly AuthorizationService _authorizationService;

        public TagService(AuthorizationService authorizationService,
            TagRepository tagRepository)
        {
            _authorizationService = authorizationService;
            _tagRepository = tagRepository;
        }

        /// <summary>
        ///     Returns if the tag name provided exists. True if yes, false if no.
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public async Task<bool> TagExistsAsync(string tagName)
        {
            var tag = await _tagRepository.FetchAsync(tagName);
            if (tag is null) return false;
            return true;
        }

        /// <summary>
        ///     Executes the tag given, and sends it to the channelID given.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ExecuteTagAsync(string tagName, Snowflake channel, MessageReference reference = null)
        {
            var msgChannel = await Bot.FetchChannelAsync(channel);
            var tag = await _tagRepository.FetchAsync(tagName);
            if (msgChannel is not IMessageChannel messageChannel)
                throw new Exception("The channel provided is not a message channel.");
            if (tag is null) return;
            if (reference == null)
                await messageChannel.SendMessageAsync(new LocalMessage().WithContent(tag.Response).WithAllowedMentions(LocalAllowedMentions.None));
            else
                await messageChannel.SendMessageAsync(new LocalMessage().WithContent(tag.Response).WithAllowedMentions(LocalAllowedMentions.ExceptEveryone)
                    .WithReference(new LocalMessageReference().WithMessageId(reference.MessageId.Value)));
        }

        public async Task<Tag> FetchTagAsync(string tagName)
        {
            return await _tagRepository.FetchAsync(tagName);
        }
        
        /// <summary>
        ///     Creates a tag with the given response.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ownerId"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task CreateTagAsync(string name, Snowflake ownerId, string response)
        {
            _authorizationService.RequireClaims(ClaimMapType.TagManage);
            var id = DatabaseUtilities.ProduceId();
            var tag = await _tagRepository.FetchAsync(name);
            if (tag is not null) throw new Exception("A tag with that name already exists.");
            using (var transaction = await _tagRepository.BeginCreateTransactionAsync())
            {
                await _tagRepository.CreateAsync(new TagCreationData
                {
                    Id = id,
                    OwnerId = ownerId,
                    Name = name,
                    Response = response
                });
                transaction.Commit();
            }
        }

        /// <summary>
        ///     Deletes the tag given by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task DeleteTagAsync(string name)
        {
            _authorizationService.RequireClaims(ClaimMapType.TagManage);
            var tags = await _tagRepository.FetchAsync(name);
            if (tags is null)
                throw new ArgumentException("That tag was not found.");
            await _tagRepository.DeleteAsync(tags);
        }

        /// <summary>
        ///     Takes an already existing tag, and edits that tag's response.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newResponse"></param>
        /// <returns></returns>
        public async Task EditTagResponseAsync(string name, string newResponse)
        {
            _authorizationService.RequireClaims(ClaimMapType.TagManage);
            var tag = await _tagRepository.FetchAsync(name);
            if (tag is null) throw new ArgumentException("The tag provided was not found.");
            await _tagRepository.UpdateResponseAsync(name, newResponse);
        }

        /// <summary>
        ///     Transfers ownership of the tag given to a new user, said user can delete/edit the tag.
        /// </summary>
        /// <param name="tagToTransfer"></param>
        /// <param name="newOwnerId"></param>
        /// <returns></returns>
        public async Task TransferTagOwnershipAsync(string tagToTransfer, Snowflake newOwnerId)
        {
            _authorizationService.RequireClaims(ClaimMapType.TagManage);
            var tag = await _tagRepository.FetchAsync(tagToTransfer);
            if (tag is null) throw new ArgumentException("The tag provided was not found.");
            await _tagRepository.UpdateOwnerAsync(tag.Name, newOwnerId);
        }
    }
}