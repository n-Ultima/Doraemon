using System;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Doraemon.Data.Models.Moderation;
using Doraemon.Data.Repositories;
using Doraemon.Services.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Doraemon.Services.Modmail
{
    [DoraemonService]
    public class SnippetService : DoraemonBotService
    {
        private readonly AuthorizationService _authorizationService;
        public SnippetService(IServiceProvider serviceProvider, AuthorizationService authorizationService)
            : base(serviceProvider)
        {
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Creates a snippet.
        /// </summary>
        /// <param name="snippetName">The name of the snippet.</param>
        /// <param name="snippetContent">The content that the snippet should hold.</param>
        /// <exception cref="Exception">Thrown if a snippet with the same name already exists.</exception>
        public async Task CreateSnippetAsync(string snippetName, string snippetContent)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailSnippetManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var snippetRepository = scope.ServiceProvider.GetRequiredService<SnippetRepository>();
                var snippetExists = await snippetRepository.FetchAsync(snippetName);
                if (snippetExists != null)
                    throw new Exception($"A snippet with this name already exists.");
                await snippetRepository.CreateAsync(new ModmailSnippetCreationData
                {
                    Name = snippetName,
                    Content = snippetContent
                });
            }
        }

        /// <summary>
        /// Fetches a snippet that has the name provided.
        /// </summary>
        /// <param name="snippetName">The name of the snippet.</param>
        /// <returns>A <see cref="ModmailSnippet"/>that matches the name provided.</returns>
        public async Task<ModmailSnippet> FetchSnippetAsync(string snippetName)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailSnippetView);
            using (var scope = ServiceProvider.CreateScope())
            {
                var snippetRepository = scope.ServiceProvider.GetRequiredService<SnippetRepository>();
                return await snippetRepository.FetchAsync(snippetName);
            }
        }

        /// <summary>
        /// Modifies the given snippet's content.
        /// </summary>
        /// <param name="snippetName">The name of the snippet to modify.</param>
        /// <param name="newContent">The new content to be applied to the snippet.</param>
        /// <exception cref="Exception">Thrown if the snippet provided does not exist.</exception>
        public async Task ModifySnippetAsync(string snippetName, string newContent)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailSnippetManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var snippetRepository = scope.ServiceProvider.GetRequiredService<SnippetRepository>();
                var snippet = await snippetRepository.FetchAsync(snippetName);
                if (snippet == null)
                {
                    throw new Exception($"The snippet provided does not exist.");
                }
                await snippetRepository.ModifyAsync(snippet, newContent);
            }
            
        }
        
        /// <summary>
        /// Deletes the modmail snippet provided.
        /// </summary>
        /// <param name="snippetName">The name of the snippet to delete.</param>
        /// <exception cref="Exception">Thrown if the snippet provided does not exist.</exception>
        public async Task DeleteModmailSnippetAsync(string snippetName)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailSnippetManage);
            using (var scope = ServiceProvider.CreateScope())
            {
                var snippetRepository = scope.ServiceProvider.GetRequiredService<SnippetRepository>();
                var snippet = await snippetRepository.FetchAsync(snippetName);
                if (snippet == null)
                    throw new Exception($"The snippet provided does not exist.");
                await snippetRepository.DeleteAsync(snippet);
            }
        }
    }
}