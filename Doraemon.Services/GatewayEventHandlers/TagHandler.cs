using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Services.PromotionServices;

namespace Doraemon.Services.GatewayEventHandlers
{
    public class TagHandler : DoraemonEventService
    {
        private readonly TagService _tagService;

        private static readonly Regex _inlineTagRegex = new(@"\$(\S+)\b");

        public TagHandler(AuthorizationService authorizationService, InfractionService infractionService, TagService tagService)
            : base(authorizationService, infractionService)
            => _tagService = tagService;

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Message is not IUserMessage message) return;
            // Make sure a bot is not attempting to use a Tag
            // Declare some context.
            var content = Regex.Replace(message.Content, @"(`{1,3}).*?(.\1)", string.Empty, RegexOptions.Singleline);
            var reference = message.Reference;
            if (reference == null)
            {
                content = Regex.Replace(content, "^>.*$", string.Empty, RegexOptions.Multiline);
                if (string.IsNullOrWhiteSpace(content)) return;
                var match = _inlineTagRegex.Match(content);
                if (!match.Success) return;
                var tagName = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(tagName)) return;
                if (!await _tagService.TagExistsAsync(tagName)) return;
                await _tagService.ExecuteTagAsync(tagName, message.ChannelId);
            }
            else
            {
                content = Regex.Replace(content, "^>.*$", string.Empty, RegexOptions.Multiline);
                if (string.IsNullOrWhiteSpace(content)) return;
                var match = _inlineTagRegex.Match(content);
                if (!match.Success) return;
                var tagName = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(tagName)) return;
                if (!await _tagService.TagExistsAsync(tagName)) return;
                await _tagService.ExecuteTagAsync(tagName, message.ChannelId, reference);
            }
        }
    }
}