using System.Collections.Generic;
using System.Linq;
using Disqord;

namespace Doraemon.Common.Extensions
{
    public static class EmbedExtensions
    {
        /// <summary>
        ///     Converts the <see cref="Embed"/> to a <see cref="LocalEmbed"/> for usage.
        /// </summary>
        /// <param name="messageEmbed">The embed to be converted.</param>
        /// <returns>A <see cref="LocalEmbed"/> that matches the <see cref="Embed"/> provided.</returns>
        public static LocalEmbed ToLocalEmbed(this IEmbed messageEmbed)
        {
            var localEmbed = new LocalEmbed();
            if (messageEmbed.Fields.Any())
            {
                List<LocalEmbedField> fields = new();
                foreach(var field in messageEmbed.Fields)
                {
                    fields.Add(new LocalEmbedField
                    {
                        Name = field.Name,
                        IsInline = field.IsInline,
                        Value = field.Value
                    });
                }
                localEmbed.WithFields(fields);
            }
            if (messageEmbed.Author != null)
            {
                var author = new LocalEmbedAuthor();
                if (messageEmbed.Author.Name != null)
                {
                    author.Name = messageEmbed.Author.Name;
                }

                if (messageEmbed.Author.Url != null)
                {
                    author.Url = messageEmbed.Author.Url;
                }

                if (messageEmbed.Author.IconUrl != null)
                {
                    author.IconUrl = messageEmbed.Author.IconUrl;
                }

                localEmbed.WithAuthor(author);
            }

            if (messageEmbed.Color != null)
            {
                localEmbed.WithColor(messageEmbed.Color);
            }

            if (messageEmbed.Description != null)
            {
                localEmbed.WithDescription(messageEmbed.Description);
            }

            if (messageEmbed.Footer != null)
            {
                var footer = new LocalEmbedFooter();
                if (messageEmbed.Footer.Text != null)
                {
                    footer.Text = messageEmbed.Footer.Text;
                }

                if (messageEmbed.Footer.IconUrl != null)
                {
                    footer.IconUrl = messageEmbed.Footer.IconUrl;
                }

                localEmbed.WithFooter(footer);
            }

            if (messageEmbed.Image != null)
            {
                if (messageEmbed.Image.Url != null)
                {
                    localEmbed.WithImageUrl(messageEmbed.Image.Url);
                }
            }

            if (messageEmbed.Thumbnail != null)
            {
                if (messageEmbed.Thumbnail.Url != null)
                {
                    localEmbed.WithThumbnailUrl(messageEmbed.Thumbnail.Url);
                }
            }

            if (messageEmbed.Title != null)
            {
                localEmbed.WithTitle(messageEmbed.Title);
            }

            if (messageEmbed.Url != null)
            {
                localEmbed.WithUrl(messageEmbed.Url);
            }

            return localEmbed;
        }
    }
}