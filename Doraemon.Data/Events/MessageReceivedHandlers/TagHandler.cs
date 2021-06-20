using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Doraemon.Data.Services;
using System.Text.RegularExpressions;

namespace Doraemon.Data.Events.MessageReceivedHandlers
{
    public class TagHandler
    {
        private static readonly Regex _inlineTagRegex = new Regex(@"\$(\S+)\b");
        public TagService _tagService;
        public TagHandler(TagService tagService)
        {
            _tagService = tagService;
        }
        /// <summary>
        /// Checks for tags inside of a message.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task CheckForTagsAsync(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }
            // Make sure a bot is not attempting to use a Tag
            if (!(arg is SocketUserMessage message)) return;
            // Declare some context.
            var content = Regex.Replace(message.Content, @"(`{1,3}).*?(.\1)", string.Empty, RegexOptions.Singleline);
            var reference = message.Reference;
            if(reference == null)
            {
                content = Regex.Replace(content, "^>.*$", string.Empty, RegexOptions.Multiline);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }
                var match = _inlineTagRegex.Match(content);
                if (!match.Success)
                {
                    return;
                }
                var tagName = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    return;
                }
                if (!await _tagService.TagExistsAsync(tagName))
                {
                    return;
                }
                await _tagService.ExecuteTagAsync(tagName, message.Channel.Id);
                return;
            }
            else
            {
                content = Regex.Replace(content, "^>.*$", string.Empty, RegexOptions.Multiline);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }
                var match = _inlineTagRegex.Match(content);
                if (!match.Success)
                {
                    return;
                }
                var tagName = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    return;
                }
                if (!await _tagService.TagExistsAsync(tagName))
                {
                    return;
                }
                await _tagService.ExecuteTagAsync(tagName, message.Channel.Id, reference);
            }
        }
    }
}
