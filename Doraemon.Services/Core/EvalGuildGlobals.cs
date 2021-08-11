using Disqord.Bot;

namespace Doraemon.Services.Core
{
    public class EvalGuildGlobals : EvalGlobals
    {
        public override DiscordGuildCommandContext Context { get; }

        public EvalGuildGlobals(DiscordGuildCommandContext context) : base(context)
        {
            Context = context;
        }
    }
}