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

        public async Task<ModmailSnippet> FetchSnippetAsync(string snippetName)
        {
            _authorizationService.RequireClaims(ClaimMapType.ModmailSnippetView);
            using (var scope = ServiceProvider.CreateScope())
            {
                var snippetRepository = scope.ServiceProvider.GetRequiredService<SnippetRepository>();
                return await snippetRepository.FetchAsync(snippetName);
            }
        }

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