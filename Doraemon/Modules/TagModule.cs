using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Data.Services;
using Doraemon.Data.Models;
using Doraemon.Data;
using System.Text.RegularExpressions;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;
using Doraemon.Data.Models.Core;
using Doraemon.Common.Attributes;

namespace Doraemon.Modules
{
    [Name("Tag")]
    [Summary("Provides utilites for using tags.")]
    [Group("tag")]
    public class TagModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Regex _tagNameRegex = new Regex(@"^\S+\b$");
        public DoraemonContext _doraemonContext;
        public TagService _tagService;
        public TagModule
        (
            DoraemonContext doraemonContext,
            TagService tagService
        )
        {
            _doraemonContext = doraemonContext;
            _tagService = tagService;
        }
        [Command("create")]
        [RequireTagAuthorization]
        [Summary("Creates a new tag, with the given response.")]
        public async Task CreateTagAsync(
            [Summary("The name of the tag to be created.")]
                string name,
            [Summary("The response that the tag should contain.")]
                [Remainder] string response)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(response))
            {
                throw new ArgumentException("The tag name/Content cannot be null or whitespaces.");
            }
            if (!_tagNameRegex.IsMatch(name))
            {
                throw new ArgumentException("The tag name cannot have punctuation.");
            }
            await _tagService.CreateTagAsync(name, Context.User.Id, response);
            await Context.AddConfirmationAsync();
        }
        [Command("list", RunMode = RunMode.Async)]
        [Priority(30)]
        [Summary("List all tags in a server.")]
        public async Task ListAllTagsAsync()
        {
            var builder = new StringBuilder();
            int num = 0;
            foreach (var tag in _doraemonContext.Tags.AsQueryable().OrderBy(x => x.Name))
            {
                num++;
                builder.AppendLine($"{num}. {tag.Name}");
            }
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("Tags")
                .WithColor(Color.DarkPurple)
                .WithDescription(builder.ToString())
                .WithFooter("Use tags by using \"!tag <tagName>\" or by doing them inline with $tagname")
                .Build();
            await ReplyAsync(embed: embed);
        }
        // Delete a tag
        [Command("delete")]
        [RequireTagAuthorization]
        [Summary("Deletes a tag.")]
        public async Task DeleteTagAsync(
            [Summary("The tag to be deleted.")]
                string tagName)
        {
            var tags = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == tagName)
                .FirstOrDefaultAsync();
            if (tags is null)
            {
                throw new ArgumentException("That tag was not found.");
            }
            else
            {
                if (tags.OwnerId != Context.User.Id)
                {
                    if (Context.User.IsStaff())
                    {
                        await _tagService.DeleteTagAsync(tagName);
                        await Context.AddConfirmationAsync();
                        return;
                    }
                    throw new Exception("You cannot delete a tag you do not own.");
                }
                await _tagService.DeleteTagAsync(tagName);
                await Context.AddConfirmationAsync();
            }
        }
        // Edit a tag's response.
        [Command("edit")]
        [Summary("Edits a tag response.")]
        [RequireTagAuthorization]
        public async Task EditTagAsync(
            [Summary("The tag to be edited.")]
                string originalTag,
            [Summary("The updated response that the tag should contain.")]
                [Remainder] string updatedResponse)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Name == originalTag);
            if (tag is null)
            {
                throw new ArgumentException("The tag provided was not found.");
            }
            else
            {
                if (tag.OwnerId != Context.User.Id)
                {
                    if (Context.User.IsStaff())
                    {
                        await _tagService.EditTagResponseAsync(originalTag, updatedResponse);
                        await Context.AddConfirmationAsync();
                    }
                }
                await _tagService.EditTagResponseAsync(originalTag, updatedResponse);
                await Context.AddConfirmationAsync();
            }
        }
        [Command]
        [Summary("Executes the given tag name.")]
        public async Task ExecuteTagAsync(
            [Summary("The tag to execute.")]
                string tagToExecute)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == tagToExecute)
                .FirstOrDefaultAsync();
            if (tag is null)
            {
                throw new ArgumentException("That tag does not exist.");
            }
            else
            {
                await _tagService.ExecuteTagAsync(tagToExecute, Context.Channel.Id);
            }
        }
        [Command("owner")]
        [Summary("Displays the owner of a tag.")]
        [Alias("ownedby")]
        public async Task DisplayTagOwnerAsync(
            [Summary("The tag to query for its owner.")]
                string query)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == query)
                .SingleOrDefaultAsync();
            if (tag is null)
            {
                throw new ArgumentException("That tag does not exist.");
            }
            var owner = Context.Guild.GetUser(tag.OwnerId);
            var embed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithAuthor(owner.Username + owner.Discriminator, owner.GetAvatarUrl() ?? owner.GetDefaultAvatarUrl())
                .WithDescription($"The tag {tag.Name}, is owned by {owner}")
                .WithFooter("Use tags by using \"!tag <tagName>\" or by doing them inline with $tagname")
                .Build();

            await ReplyAsync(embed: embed);

        }
        [Command("transfer")]
        [RequireTagAuthorization]
        [Summary("Transfers ownership of a tag to a new user.")]
        public async Task TransferTagOwnershipAsync(
            [Summary("The tag to transfer.")]
                string tagName,
            [Summary("The new owner of the tag.")]
                SocketGuildUser newOwner)
        {
            var tag = await _doraemonContext
                .Set<Tag>()
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tag.OwnerId != Context.User.Id)
            {
                throw new Exception("You do not own the tag, so I can't transfer ownership.");
            }
            await _tagService.TransferTagOwnershipAsync(tag.Name, newOwner.Id);
            await Context.AddConfirmationAsync();
        }
    }
}
