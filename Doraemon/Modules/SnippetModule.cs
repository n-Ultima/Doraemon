using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Parsers;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Modmail;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("Snippet")]
    [Description("Provides utilities for managing modmail snippets")]
    [Group("snippet", "snippets")]
    public class SnippetModule : DoraemonGuildModuleBase
    {
        private readonly SnippetService _snippetService;
        private readonly AuthorizationService _authorizationService;
        private readonly ModmailTicketService _modmailTicketService;

        public SnippetModule(SnippetService snippetService, AuthorizationService authorizationService, ModmailTicketService modmailTicketService)
        {
            _snippetService = snippetService;
            _authorizationService = authorizationService;
            _modmailTicketService = modmailTicketService;
        }
        [Command("create")]
        [Description("Creates a snippet")]
        [RequireClaims(ClaimMapType.ModmailSnippetManage)]
        public async Task<DiscordCommandResult> CreateSnippetAsync(
            [Description("The name of the snippet.")]
                string snippetName,
            [Description("The content that the snippet will contain.")] [Remainder]
                string content)
        {
            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet != null)
                throw new Exception($"A snippet with that name already exists.");
            await _snippetService.CreateSnippetAsync(snippetName, content);
            return Confirmation();
        }

        [Command("edit", "modify")]
        [Description("Modifies the given snippet.")]
        public async Task<DiscordCommandResult> ModifySnippetAsync(
            [Description("The name of the snippet.")]
                string name,
            [Description("The content new content for the snippet.")] [Remainder]
                string content)
        {
            var snippet = await _snippetService.FetchSnippetAsync(name);
            if (snippet == null)
                throw new Exception($"The snippet provided doesn't exist.");
            if (snippet.Content.Equals(content, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"You can't edit a snippet, with the same content?");
            await _snippetService.ModifySnippetAsync(name, content);
            return Confirmation();
        }

        [Command("delete")]
        [Description("Deletes the given snippet.")]
        [RequireClaims(ClaimMapType.ModmailSnippetManage)]
        public async Task<DiscordCommandResult> DeleteSnippetAsync(
            [Description("The name of the snippet to delete.")]
            string snippetName)
        {
            await _snippetService.DeleteModmailSnippetAsync(snippetName);
            return Confirmation();
        }
        
        [Command("preview")]
        [Description("Preview a snippets content.")]
        [RequireClaims(ClaimMapType.ModmailSnippetView)]

        public async Task<DiscordCommandResult> DisplayPreviewAsync(
            [Description("The name of the preview.")]
                string snippetName)
        {
            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
                throw new Exception($"The snippet provided does not exist.");
            var embed = new LocalEmbed()
                .WithTitle($"{snippet.Name}")
                .WithColor(DColor.Red)
                .WithDescription($"Snippet Content: {snippet.Content}")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter("Snippet not sent");
            return Response(embed);
        }

        [Command]
        [RequireClaims(ClaimMapType.ModmailRespond)]
        [Description("Sends the snippet's provided text to the corresponding DM channel.")]
        public async Task<DiscordCommandResult> SendSnippetAsync(
            [Description("The name of the snippet.")] [Remainder]
                string snippetName)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailRespond);
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(Context.ChannelId);
            if (modmailTicket == null)
            {
                return await DisplayPreviewAsync(snippetName);
            }
            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
                throw new Exception($"The snippet provided does not exist.");
            var guildUserHighestRole = Context.Author.GetRoles()
                .OrderByDescending(x => x.Value.Position)
                .Select(x => x.Value.Name)
                .First();
            var embed = new LocalEmbed()
                .WithColor(DColor.Green)
                .WithAuthor(Context.Message.Author)
                .WithDescription(snippet.Content)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(guildUserHighestRole);
            await Bot.SendMessageAsync(modmailTicket.DmChannelId, new LocalMessage()
                .WithEmbeds(embed));
            var success = new LocalEmbed()
                .WithDescription($"Here's what was sent to the user:\n```\n{snippet.Content}\n```")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithColor(DColor.Gold)
                .WithTitle("Snippet sent successfully.");
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, Context.Author.Id, $"{Context.Author.Tag} used snippet: {snippet.Name} - {snippet.Content}");
            return Response(success);
        }
    }
}