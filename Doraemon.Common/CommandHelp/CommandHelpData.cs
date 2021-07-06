using System.Collections.Generic;
using System.Linq;
using Discord.Commands;

namespace Doraemon.Common.CommandHelp
{
    public class CommandHelpData
    {
        /// <summary>
        ///     The name of the command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The summary of the command.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        ///     A collection of aliases that the command has.
        /// </summary>
        public IReadOnlyCollection<string> Aliases { get; set; }

        /// <summary>
        ///     The parameters that the command takes.
        /// </summary>
        public IReadOnlyCollection<ParameterHelpData> Parameters { get; set; }

        public static CommandHelpData FromCommandInfo(CommandInfo command)
        {
            var ret = new CommandHelpData
            {
                Name = command.Name,
                Summary = command.Summary,
                Aliases = command.Aliases,
                Parameters = command.Parameters
                    .Select(x => ParameterHelpData.FromParameterInfo(x))
                    .ToArray()
            };

            return ret;
        }
    }
}