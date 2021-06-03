using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Doraemon.Common.CommandHelp
{
    public class CommandHelpData
    {

        public string Name { get; set; }

        public string Summary { get; set; }

        public IReadOnlyCollection<string> Aliases { get; set; }

        public IReadOnlyCollection<ParameterHelpData> Parameters { get; set; }

        public static CommandHelpData FromCommandInfo(CommandInfo command)
        {
            var ret = new CommandHelpData()
            {
                Name = command.Name,
                Summary = command.Summary,
                Aliases = command.Aliases,
                Parameters = command.Parameters
                    .Select(x => ParameterHelpData.FromParameterInfo(x))
                    .ToArray(),
            };

            return ret;
        }

    }
}
