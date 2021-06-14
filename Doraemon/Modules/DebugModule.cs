using System;
using System.Threading.Tasks;
using Discord.Commands;
using Doraemon.Common.CommandHelp;

namespace Doraemon.Modules
{
    [Name("Debug")]
    [Summary("Used to debug multipe things involving Doraemon.")]
    [HiddenFromHelp]
    public class DebugModule : ModuleBase
    {
        [Command("throw")]
        [Summary("Throws an error")]
        public async Task ThrowAsync(
            [Summary("The error to throw.")]
                [Remainder] string error)
        {
            throw new Exception(error);
        }
    }
}
