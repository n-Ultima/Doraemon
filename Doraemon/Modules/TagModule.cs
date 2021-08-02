//using System;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;
//using Discord.WebSocket;
//using Doraemon.Common.Extensions;
//using Doraemon.Data;
//using Doraemon.Data.Models;
//using Doraemon.Data.Models.Core;
//using Doraemon.Services.PromotionServices;
//using Interactivity;
//using Interactivity.Pagination;
//using Microsoft.EntityFrameworkCore;
//
//namespace Doraemon.Modules
//{
//    [Name("Tag")]
//    [Summary("Provides utilites for using tags.")]
//    [Group("tag")]
//    [Alias("tags")]
//    public class TagModule : ModuleBase<SocketCommandContext>
//    {
//        private static readonly Regex _tagNameRegex = new(@"^\S+\b$");
//        public DoraemonContext _doraemonContext;
//        public TagService _tagService;
//
//        public TagModule
//        (
//            DoraemonContext doraemonContext,
//            TagService tagService,
//            InteractivityService interactivity
//        )
//        {
//            _doraemonContext = doraemonContext;
//            _tagService = tagService;
//            _interactivity = interactivity;
//        }
//
//        private InteractivityService _interactivity { get; set; }
//
//        [Command("create")]
//        [Summary("Creates a new tag, with the given response.")]
//        public async Task CreateTagAsync(
//            [Summary("The name of the tag to be created.")]
//                string name,
//            [Summary("The response that the tag should contain.")] [Remainder]
//                string response)
//        {
//            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(response))
//                throw new ArgumentException("The tag name/Content cannot be null or whitespaces.");
//            if (!_tagNameRegex.IsMatch(name)) throw new ArgumentException("The tag name cannot have punctuation.");
//            await _tagService.CreateTagAsync(name, Context.User.Id, response);
//            await Context.AddConfirmationAsync();
//        }
//
//        // Delete a tag=
//        [Command("delete")]
//        [Summary("Deletes a tag.")]
//        public async Task DeleteTagAsync(
//            [Summary("The tag to be deleted.")]
//                string tagName)
//        {
//            var tags = await _tagService.FetchTagAsync(tagName);
//            if (tags is null) throw new ArgumentException("That tag was not found.");
//
//            if (tags.OwnerId != Context.User.Id)
//            {
//                if ((Context.User as SocketGuildUser).GuildPermissions.ManageMessages)
//                {
//                    await _tagService.DeleteTagAsync(tagName, Context.User.Id);
//                    await Context.AddConfirmationAsync();
//                    return;   
//                }
//
//
//                throw new UnauthorizedAccessException("You cannot delete a tag you do not own.");
//            }
//
//            await _tagService.DeleteTagAsync(tagName, Context.User.Id);
//            await Context.AddConfirmationAsync();
//        }
//
//        // Edit a tag's response.
//        [Command("edit")]
//        [Summary("Edits a tag response.")]
//        public async Task EditTagAsync(
//            [Summary("The tag to be edited.")] 
//                string originalTag,
//            [Summary("The updated response that the tag should contain.")] [Remainder]
//                string updatedResponse)
//        {
//            var tag = await _tagService.FetchTagAsync(originalTag);
//            if (tag is null) throw new ArgumentException("The tag provided was not found.");
//
//            if (tag.OwnerId != Context.User.Id)
//            {
//                throw new InvalidOperationException($"You cannot edit tags you don't own.");
//
//            }
//                
//
//            await _tagService.EditTagResponseAsync(originalTag, updatedResponse, Context.User.Id);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command(RunMode = RunMode.Async)]
//        [Summary("Executes the given tag name.")]
//        public async Task ExecuteTagAsync(
//            [Summary("The tag to execute.")] 
//                string tagToExecute)
//        {
//            var tag = await _tagService.FetchTagAsync(tagToExecute);
//            if (tag is null)
//                throw new Exception("The tag provided does not exist.");
//            await _tagService.ExecuteTagAsync(tagToExecute, Context.Channel.Id);
//        }
//
//        [Command("owner")]
//        [Summary("Displays the owner of a tag.")]
//        [Alias("ownedby")]
//        public async Task DisplayTagOwnerAsync(
//            [Summary("The tag to query for its owner.")]
//                string query)
//        {
//            var tag = await _tagService.FetchTagAsync(query);
//            if (tag is null) throw new ArgumentException("That tag does not exist.");
//            var owner = Context.Guild.GetUser(tag.OwnerId);
//            var embed = new EmbedBuilder()
//                .WithColor(Color.DarkPurple)
//                .WithAuthor(owner.GetFullUsername(), owner.GetDefiniteAvatarUrl())
//                .WithDescription($"The tag {tag.Name}, is owned by {owner}")
//                .WithFooter("Use tags by using \"!tag <tagName>\" or by doing them inline with $tagname")
//                .Build();
//
//            await ReplyAsync(embed: embed);
//        }
//
//        [Command("transfer")]
//        [Summary("Transfers ownership of a tag to a new user.")]
//        public async Task TransferTagOwnershipAsync(
//            [Summary("The tag to transfer.")] 
//                string tagName,
//            [Summary("The new owner of the tag.")] 
//                SocketGuildUser newOwner)
//        {
//            var tag = await _tagService.FetchTagAsync(tagName);
//            if (tag.OwnerId != Context.User.Id)
//                throw new Exception("You do not own the tag, so I can't transfer ownership.");
//            await _tagService.TransferTagOwnershipAsync(tag.Name, newOwner.Id, Context.User.Id);
//            await Context.AddConfirmationAsync();
//        }
//
//        [Command(RunMode = RunMode.Async)]
//        [Priority(100)]
//        [Alias("list")]
//        public async Task ListAsync()
//        {
//            var tags = await _doraemonContext.Tags.AsQueryable().OrderBy(x => x.Name).ToListAsync();
//            var paginator = new LazyPaginatorBuilder()
//                .WithUsers(Context.User)
//                .WithMaxPageIndex((int) Math.Ceiling(tags.Count / 20d))
//                .WithPageFactory(page =>
//                {
//                    var b = new StringBuilder();
//                    foreach (var tag in tags.Skip(10 * page).Take(10)) b.AppendLine($"{tag.Name}");
//                    return Task.FromResult(new PageBuilder()
//                        .WithColor(Color.Blue)
//                        .WithDescription(b.ToString())
//                        .WithFooter($"{tags.Count()} entries.")
//                        .WithTitle("Tags"));
//                })
//                .WithFooter(PaginatorFooter.PageNumber)
//                .WithDefaultEmotes()
//                .Build();
//            await _interactivity.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(1));
//        }
//    }
//}