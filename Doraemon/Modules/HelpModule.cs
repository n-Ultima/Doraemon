using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Doraemon.Common.CommandHelp;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Humanizer;
using Humanizer.Localisation;


namespace Doraemon.Modules
{
    [Name("Help")]
    [Group("help")]
    [Summary("Used for helping users.")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        [Flags]
        public enum HelpDataType
        {
            Command = 1 << 1,
            Module = 1 << 2
        }

        private static readonly Regex _userMentionRegex = new("<@!?(?<Id>[0-9]+)>", RegexOptions.Compiled);
        private static readonly Regex _roleMentionRegex = new("<@&(?<Id>[0-9]+)>", RegexOptions.Compiled);
        private readonly ICommandHelpService _commandHelpService;
        private readonly CommandService _service;

        public HelpModule(CommandService service, ICommandHelpService commandHelpService)
        {
            _service = service;
            _commandHelpService = commandHelpService;
        }

        [Command]
        [Summary("Displays help information.")]
        public async Task DisplayHelpAsync()
        {
            var modules = _commandHelpService.GetModuleHelpData()
                .Select(x => x.Name)
                .OrderBy(x => x);
            var descriptionBuilder = new StringBuilder()
                .AppendLine("Modules:")
                .AppendJoin(", ", modules)
                .AppendLine()
                .AppendLine()
                .AppendLine("Use \"!help dm\" to have everything dm'd to you(a lot).")
                .AppendLine("Use \"!help <moduleName> to have a list of commands in that module.");
            var embed = new EmbedBuilder()
                .WithTitle("Help")
                .WithDescription(descriptionBuilder.ToString());
            await ReplyAsync(embed: embed.Build());
        }

        [Command("dm")]
        [Priority(15)]
        [Summary("Spams the user's DMs with a list of every command available.")]
        public async Task HelpDMAsync()
        {
            var userDM = await Context.User.GetOrCreateDMChannelAsync();

            foreach (var module in _commandHelpService.GetModuleHelpData().OrderBy(x => x.Name))
            {
                var embed = GetEmbedForModule(module);

                try
                {
                    await userDM.SendMessageAsync(embed: embed.Build());
                }
                catch (HttpException ex) when (ex.DiscordCode == 50007)
                {
                    await ReplyAsync(
                        $"You have private messages for this server disabled, {Context.User.Mention}. Please enable them so that I can send you help.");
                    return;
                }
            }

            await ReplyAsync($"Check your private messages, {Context.User.Mention}.");
        }

        [Command]
        [Summary("Retrieves help from a specific module or command.")]
        [Priority(-10)]
        public async Task HelpAsync(
            [Remainder] [Summary("Name of the module or command to query.")]
                string query)
        {
            await HelpAsync(query, HelpDataType.Command | HelpDataType.Module);
        }

        [Command("module")]
        [Alias("modules")]
        [Summary("Retrieves help from a specific module. Useful for modules that have an overlapping command name.")]
        public async Task HelpModuleAsync(
            [Summary("Name of the module to query.")][Remainder]
                string query)
        {
            await HelpAsync(query, HelpDataType.Module);
        }

        [Command("command")]
        [Alias("commands")]
        [Summary("Retrieves help from a specific command. Useful for commands that have an overlapping module name.")]
        public async Task HelpCommandAsync(
            [Summary("Name of the module to query.")][Remainder]
                string query)
        {
            await HelpAsync(query, HelpDataType.Command);
        }

        private async Task HelpAsync(string query, HelpDataType type)
        {
            var sanitizedQuery = FormatUtilities.SanitizeAllMentions(query);

            if (TryGetEmbed(query, type, out var embed))
            {
                await ReplyAsync($"Results for \"{sanitizedQuery}\":", embed: embed.Build());
                return;
            }

            await ReplyAsync($"Sorry, I couldn't find help related to \"{sanitizedQuery}\".");
        }

        private bool TryGetEmbed(string query, HelpDataType queries, out EmbedBuilder embed)
        {
            embed = null;

            // Prioritize module over command.
            if (queries.HasFlag(HelpDataType.Module))
            {
                var byModule = _commandHelpService.GetModuleHelpData(query);
                if (byModule != null)
                {
                    embed = GetEmbedForModule(byModule);
                    return true;
                }
            }

            if (queries.HasFlag(HelpDataType.Command))
            {
                var byCommand = _commandHelpService.GetCommandHelpData(query);
                if (byCommand != null)
                {
                    embed = GetEmbedForCommand(byCommand);
                    return true;
                }
            }

            return false;
        }

        private EmbedBuilder GetEmbedForCommand(CommandHelpData command)
        {
            return AddCommandFields(new EmbedBuilder(), command);
        }

        private EmbedBuilder GetEmbedForModule(ModuleHelpData module)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle($"Module: {module.Name}")
                .WithDescription(module.Summary);

            foreach (var command in module.Commands) AddCommandFields(embedBuilder, command);

            return embedBuilder;
        }

        private EmbedBuilder AddCommandFields(EmbedBuilder embedBuilder, CommandHelpData command)
        {
            var summaryBuilder = new StringBuilder(command.Summary ?? "No summary.").AppendLine();
            var name = command.Aliases.FirstOrDefault();
            AppendParameters(summaryBuilder, command.Parameters);
            AppendAliases(summaryBuilder, command.Aliases.Where(x => !x.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList());
            
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithName($"Command: !{name} {GetParams(command)}")
                .WithValue(summaryBuilder.ToString()));

            return embedBuilder;
        }

        private StringBuilder AppendAliases(StringBuilder stringBuilder, IReadOnlyCollection<string> aliases)
        {
            if (aliases.Count == 0)
                return stringBuilder;

            stringBuilder.AppendLine(Format.Bold("Aliases:"));

            foreach (var alias in CollapsePlurals(aliases))
            {
                stringBuilder.AppendLine($"• {alias}");
            }

            return stringBuilder;
        }
        private StringBuilder AppendParameters(StringBuilder stringBuilder,
            IReadOnlyCollection<ParameterHelpData> parameters)
        {
            if (parameters.Count == 0)
                return stringBuilder;

            stringBuilder.AppendLine(Format.Bold("Parameters:"));

            foreach (var parameter in parameters)
                if (!(parameter.Summary is null))
                    stringBuilder.AppendLine($"• {Format.Bold(parameter.Name)}: {parameter.Summary}");

            return stringBuilder;
        }

        private string GetParams(CommandHelpData info)
        {
            var sb = new StringBuilder();

            foreach (var parameter in info.Parameters)
                if (parameter.IsOptional)
                    sb.Append($"[{parameter.Name}]");
                else
                    sb.Append($"<{parameter.Name}>");

            return sb.ToString();
        }

        public IReadOnlyCollection<string> CollapsePlurals(IReadOnlyCollection<string> sentences)
        {
            var splitIntoWords = sentences.Select(x => x.Split(" ", StringSplitOptions.RemoveEmptyEntries));

            var withSingulars = splitIntoWords.Select(x => 
            (
                Singluar: sentences.Select(y => y.Singularize(false)).ToArray(),
                Value: x
            ));

            var groupedBySingulars = withSingulars.GroupBy(x => x.Singluar, x => x.Value, new SequenceEqualityComparer<string>());

            var withDistinctParts = new HashSet<string>[groupedBySingulars.Count()][];
            
            foreach (var (singular, singularIndex) in groupedBySingulars.AsIndexable())
            {
                var parts = new HashSet<string>[singular.Key.Count];

                for (var i = 0; i < parts.Length; i++)
                    parts[i] = new HashSet<string>();

                foreach (var variation in singular)
                {
                    foreach (var (part, partIndex) in variation.AsIndexable())
                    {
                        parts[partIndex].Add(part);
                    }
                }

                withDistinctParts[singularIndex] = parts;
            }

            var parenthesized = new string[withDistinctParts.Length][];

            foreach (var (alias, aliasIndex) in withDistinctParts.AsIndexable())
            {
                parenthesized[aliasIndex] = new string[alias.Length];

                foreach (var (word, wordIndex) in alias.AsIndexable())
                {
                    if (word.Count == 2)
                    {
                        var indexOfDifference = word.First()
                            .ZipOrDefault(word.Last())
                            .AsIndexable()
                            .First(x => x.Value.First != x.Value.Second)
                            .Index;

                        var longestForm = word.First().Length > word.Last().Length
                            ? word.First()
                            : word.Last();

                        parenthesized[aliasIndex][wordIndex] = $"{longestForm.Substring(0, indexOfDifference)}({longestForm.Substring(indexOfDifference)})";
                    }
                    else
                    {
                        parenthesized[aliasIndex][wordIndex] = word.Single();
                    }
                }
            }

            var formatted = parenthesized.Select(aliasParts => string.Join(" ", aliasParts)).ToArray();

            return formatted;
        }
    }
    public static class EnumerableExtensions
    {
        public static IEnumerable<(T Value, int Index)> AsIndexable<T>(this IEnumerable<T> source)
        {
            var index = 0;

            foreach (var item in source)
            {
                yield return (item, index);
                index++;
            }
        }
        
        public static IEnumerable<(TFirst First, TSecond Second)> ZipOrDefault<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            using var e1 = first.GetEnumerator();
            using var e2 = second.GetEnumerator();

            while (true)
            {
                var e1Moved = e1.MoveNext();
                var e2Moved = e2.MoveNext();

                if (!e1Moved && !e2Moved)
                    break;

                yield return
                (
                    e1Moved ? e1.Current : default,
                    e2Moved ? e2.Current : default
                );
            }
        }
    }
}