namespace Doraemon.Data.Models.Moderation
{
    public class ModmailSnippetCreationData
    {
        public string Name { get; set; }
        public string Content { get; set; }

        internal ModmailSnippet ToEntity()
            => new ModmailSnippet()
            {
                Name = Name,
                Content = Content
            };
    }
}