using System.Text.RegularExpressions;

namespace Doraemon.Services.Core
{
    public class EvalService
    {
        public static readonly string[] EvalNamespaces = new[]
        {
            "System", "System.Diagnostics", "System.Threading.Tasks", "System.Linq",
            "System.Text", "System.Collections.Generic", "System.Reflection",
            "Disqord", "Disqord.Rest", "Disqord.Bot",
            "Disqord.Rest.Api", "Disqord.Gateway", "Disqord.Gateway.Api",
            "Disqord.Gateway.Default", "Qmmands", "Microsoft.Extensions.DependencyInjection",
            "Doraemon.Modules", "Doraemon", "Doraemon.Services", "Doraemon.Common"
        };
        
        private static readonly Regex SingleCodeBlockRegex = new Regex(@"```(?<language>(?:\w+)?)(?:\n)?(?<code>.*?)```", RegexOptions.Compiled | RegexOptions.Singleline);

        private static bool TryMatchCode(string text, out (string Language, string Code) code)
        {
            var match = SingleCodeBlockRegex.Match(text);
            if (match.Success)
            {
                code = (match.Groups["language"].Value, match.Groups["code"].Value);
                return true;
            }
            code = default;
            return false;
        }
        public static string TrimCode(string text)
            => (TryMatchCode(text, out var match) ? match.Code : text.Trim('`'))
                .Trim(' ');

        public static string ValidateCode(string text)
        {
            text = TrimCode(text);
            if (!text.EndsWith(';'))
                text += ';';

            return text;
        }
    }
}