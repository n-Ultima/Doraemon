using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.PromotionServices
{
    [DoraemonService]
    public class TagService : DoraemonBotService
    {
        private readonly AuthorizationService _authorizationService;
        private readonly ClaimService _claimService;

        public TagService(AuthorizationService authorizationService, IServiceProvider serviceProvider, ClaimService claimService)
            : base(serviceProvider)
        {
            _authorizationService = authorizationService;
            _claimService = claimService;
        }

        /// <summary>
        ///     Returns if the tag name provided exists. True if yes, false if no.
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public async Task<bool> TagExistsAsync(string tagName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                var tag = await tagRepository.FetchAsync(tagName);
                if (tag is null) return false;
                return true;
            }
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
            using (var scope = ServiceProvider.CreateScope())
            {
                var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                var tag = await tagRepository.FetchAsync(tagName);
                if (msgChannel is not IMessageChannel messageChannel)
                    throw new Exception("The channel provided is not a message channel.");
                if (tag is null) return;
                if (reference == null)
                    await messageChannel.SendMessageAsync(new LocalMessage().WithContent(tag.Response).WithAllowedMentions(LocalAllowedMentions.None));
                else
                    await messageChannel.SendMessageAsync(new LocalMessage().WithContent(tag.Response).WithAllowedMentions(LocalAllowedMentions.ExceptEveryone)
                        .WithReference(new LocalMessageReference().WithMessageId(reference.MessageId.Value)));
            }
            
        }

        /// <summary>
        /// Returns a tag by the given name.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        /// <returns>A <see cref="Tag"/> with the given name.</returns>
        public async Task<Tag> FetchTagAsync(string tagName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                return await tagRepository.FetchAsync(tagName);
            } 
        }

        /// <summary>
        /// Gets a list of all the tags in the database, ordered by name.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{Tag}"/> of all existing tags.</returns>
        public async Task<IEnumerable<Tag>> FetchTagsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                return await tagRepository.FetchAllTagsAsync();
            } 
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
            _authorizationService.RequireClaims(ClaimMapType.CreateTag);
            var id = DatabaseUtilities.ProduceId();
            using (var scope = ServiceProvider.CreateScope())
            {
                var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                var tag = await tagRepository.FetchAsync(name);
                if (tag is not null) throw new Exception("A tag with that name already exists.");
                {
                    await tagRepository.CreateAsync(new TagCreationData
                    {
                        Id = id,
                        OwnerId = ownerId,
                        Name = name,
                        Response = response
                    });
                }
            }
        }
            /// <summary>
            ///     Deletes the tag given by name.
            /// </summary>
            /// <param name="name">The name of the tag.</param>
            /// <param name="requestor">The user requesting the tag.</param>
            /// <returns></returns>
            public async Task DeleteTagAsync(string name, IMember requestor)
            {
                _authorizationService.RequireClaims(ClaimMapType.MaintainOwnedTag);
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                    var tags = await tagRepository.FetchAsync(name);
                    if (tags is null)
                        throw new ArgumentException("That tag was not found.");
                    if (!await CanUserManageTag(tags, requestor))
                        throw new Exception($"You cannot manage this tag.");
                    await tagRepository.DeleteAsync(tags);
                }
            }

            /// <summary>
            ///     Takes an already existing tag, and edits that tag's response.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="newResponse"></param>
            /// <returns></returns>
            public async Task EditTagResponseAsync(string name, IMember requestor, string newResponse)
            {
                _authorizationService.RequireClaims(ClaimMapType.MaintainOwnedTag);
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                    var tag = await tagRepository.FetchAsync(name);
                    if (tag is null) throw new ArgumentException("The tag provided was not found.");
                    if (!await CanUserManageTag(tag, requestor))
                        throw new Exception($"You cannot manage this tag.");
                    await tagRepository.UpdateResponseAsync(name, newResponse);
                }
            }

            /// <summary>
            ///     Transfers ownership of the tag given to a new user, said user can delete/edit the tag.
            /// </summary>
            /// <param name="tagToTransfer"></param>
            /// <param name="newOwnerId"></param>
            /// <returns></returns>
            public async Task TransferTagOwnershipAsync(string tagToTransfer, IMember requestor, Snowflake newOwnerId)
            {
                _authorizationService.RequireClaims(ClaimMapType.MaintainOwnedTag);
                using (var scope = ServiceProvider.CreateScope())
                {
                    var tagRepository = scope.ServiceProvider.GetRequiredService<TagRepository>();
                    var tag = await tagRepository.FetchAsync(tagToTransfer);
                    if (tag is null) throw new ArgumentException("The tag provided was not found.");
                    if (!await CanUserManageTag(tag, requestor))
                        throw new Exception($"You cannot manage this tag.");
                    await tagRepository.UpdateOwnerAsync(tag.Name, newOwnerId);
                }
            }

            public async Task<bool> CanUserManageTag(Tag tag, IMember member)
            {
                if (tag.OwnerId == member.Id)
                    return true;
                var guild = Bot.GetGuild(member.GuildId);
                if (guild.OwnerId == member.Id)
                    return true;
                var userClaims = await _claimService.FetchAllClaimsForUserAsync(member.Id, member.RoleIds);
                if (!userClaims.Any())
                    return false;
                if (userClaims.Contains(ClaimMapType.MaintainOtherUserTag))
                    return true;
                return false;
            }
        }
    }