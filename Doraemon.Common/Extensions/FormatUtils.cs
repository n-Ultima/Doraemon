namespace Doraemon.Common.Extensions
{
    public static class Format
    {
        public static string Bold(string input) => $"**{input}**";

        public static string CodeBlock(string input) => $"`{input}`";

        public static string Italics(string input) => $"*{input}*";

        public static string ItalicBold(string input) => $"***{input}***";
    }
}