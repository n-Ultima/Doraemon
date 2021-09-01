using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Extensions.Interactivity;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Services.PromotionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Tag")]
    [Description("Provides utilites for using tags.")]
    [Group("tag", "tags")]
    public class TagModule : DoraemonGuildModuleBase
    {
        private static readonly Regex _tagNameRegex = new(@"^\S+\b$");
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        private readonly TagService _tagService;

        public TagModule
        (
            TagService tagService
        )
        {
            _tagService = tagService;
        }

        [Command("create")]
        [RequireClaims(ClaimMapType.MaintainOwnedTag)]
        [Description("Creates a new tag, with the given response.")]
        public async Task<DiscordCommandResult> CreateTagAsync(
            [Description("The name of the tag to be created.")]
                string name,
            [Description("The response that the tag should contain.")] [Remainder]
                string response)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(response))
                throw new ArgumentException("The tag name/Content cannot be null or whitespaces.");
            if (!_tagNameRegex.IsMatch(name)) throw new ArgumentException("The tag name cannot have punctuation.");
            await _tagService.CreateTagAsync(name, Context.Author.Id, response);
            return Confirmation();
        }

        // Delete a tag=
        [Command("delete")]
        [RequireClaims(ClaimMapType.MaintainOwnedTag)]
        [Qmmands.Description("Deletes a tag.")]
        public async Task<DiscordCommandResult> DeleteTagAsync(
            [Description("The tag to be deleted.")]
                string tagName)
        {
            var tags = await _tagService.FetchTagAsync(tagName);
            if (tags is null) throw new ArgumentException("That tag was not found.");

            await _tagService.DeleteTagAsync(tagName, Context.Author);
            return Confirmation();
        }

        // Edit a tag's response.
        [Command("edit")]
        [RequireClaims(ClaimMapType.MaintainOwnedTag)]
        [Description("Edits a tag response.")]
        public async Task<DiscordCommandResult> EditTagAsync(
            [Description("The tag to be edited.")]
                string originalTag,
            [Description("The updated response that the tag should contain.")] [Remainder]
                string updatedResponse)
        {
            var tag = await _tagService.FetchTagAsync(originalTag);
            if (tag is null) throw new ArgumentException("The tag provided was not found.");

            if (tag.OwnerId != Context.Author.Id)
            {
                throw new InvalidOperationException($"You cannot edit tags you don't own.");
            }

            await _tagService.EditTagResponseAsync(originalTag, Context.Author, updatedResponse);
            return Confirmation();
        }

        [Command]
        [Description("Executes the given tag name.")]
        public async Task ExecuteTagAsync(
            [Description("The tag to execute.")]
                string tagToExecute)
        {
            var tag = await _tagService.FetchTagAsync(tagToExecute);
            if (tag is null)
                throw new Exception("The tag provided does not exist.");
            await _tagService.ExecuteTagAsync(tagToExecute, Context.Channel.Id);
        }

        [Command("owner", "ownedby")]
        [Description("Displays the owner of a tag.")]
        public async Task<DiscordCommandResult> DisplayTagOwnerAsync(
            [Description("The tag to query for its owner.")]
            string query)
        {
            var tag = await _tagService.FetchTagAsync(query);
            if (tag is null) throw new ArgumentException("That tag does not exist.");
            var owner = Context.Guild.GetMember(tag.OwnerId);
            var embed = new LocalEmbed()
                .WithColor(DColor.DarkPurple)
                .WithAuthor(owner)
                .WithDescription($"The tag {tag.Name}, is owned by {owner}")
                .WithFooter($"Use tags by using \"{DoraemonConfig.Prefix}tag <tagName>\" or by doing them inline with $tagname");
            return Response(embed);
        }

        [Command("transfer")]
        [RequireClaims(ClaimMapType.MaintainOwnedTag)]
        [Description("Transfers ownership of a tag to a new user.")]
        public async Task<DiscordCommandResult> TransferTagOwnershipAsync(
            [Description("The tag to transfer.")]
                string tagName,
            [Description("The new owner of the tag.")]
                IMember newOwner)
        {
            var tag = await _tagService.FetchTagAsync(tagName);
            if (tag.OwnerId != Context.Author.Id)
                throw new Exception("You do not own the tag, so I can't transfer ownership.");
            await _tagService.TransferTagOwnershipAsync(tag.Name, Context.Author, newOwner.Id);
            return Confirmation();
        }

        [Command("claim")]
        [Description("Allows users to claim tags whose owners are no longer inside the guild.")]
        public async Task<DiscordCommandResult> ClaimTagAsync(string tagName)
        {
            var tag = await _tagService.FetchTagAsync(tagName);
            if (tag == null)
                throw new Exception($"The tag provided doesn't exist.");
            var guildMember = Context.Guild.GetMember(tag.OwnerId);
            if (guildMember != null)
                throw new Exception($"The tag's owner is still in the server.");
            using (var scope = Context.Bot.Services.CreateScope())
            {
                var doraemonContext = scope.ServiceProvider.GetRequiredService<DoraemonContext>();
                tag.OwnerId = Context.Author.Id;
                await doraemonContext.SaveChangesAsync();
            }
            return Confirmation();
        }

        [Command("", "list")]
        [Description("Returns a select menu that lists 10 tags per page if applicable.")]
        [Priority(100)]
        public async Task<DiscordCommandResult> ListAsync()
        {
            var tags = await _tagService.FetchTagsAsync();
            var tagNames = tags.Select(x => x.Name).ToArray();
            var pageProvider = new ArrayPageProvider<string>(tagNames, itemsPerPage:
                tagNames.Length >= 10
                    ? 10
                    : tagNames.Length);
            return Pages(pageProvider);
        }
    }
}