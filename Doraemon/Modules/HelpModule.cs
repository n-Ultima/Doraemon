using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Humanizer;
using Qmmands;


namespace Doraemon.Modules
{
    [Name("Help")]
    [Group("help")]
    [Description("Used for helping users.")]
    public class HelpModule : DoraemonGuildModuleBase
    {
        private readonly CommandService _commandService;
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();

        public HelpModule(CommandService commandService)
        {
            _commandService = commandService;
        }
        
        [Flags]
        public enum HelpDataType
        {
            Command = 1 << 1,
            Module = 1 << 2
        }
        
        [Command]
        [Description("Displays all modules.")]
        public DiscordCommandResult DisplayModulesAsync()
        {
            var modules = _commandService.GetAllModules().Where(x => x.Commands.Any());
            var builder = new StringBuilder();
            var humanizedModules = modules.Humanize();
            builder.Append(humanizedModules);
            return Response(new LocalEmbed()
                .WithTitle("Help")
                .WithDescription(humanizedModules + "\n\n")
                .WithFooter($"Use \"{DoraemonConfig.Prefix}help <Module>\" to get a list of commands in that module. Use \"{DoraemonConfig.Prefix}help dm\" to get a list of all commands DM'd to them(a lot)."));
        }

        [Command("dm")]
        [Priority(10)]
        [Description("DMs the executor a list of all commands.")]
        public async Task<DiscordCommandResult> HelpDmAsync()
        {
            foreach (var module in _commandService.GetAllModules().OrderBy(x => x.Name))
            {
                var embed = GetEmbedForModule(module);
                try
                {
                    await Context.Author.SendMessageAsync(new LocalMessage().WithEmbeds(embed));
                }
                catch (RestApiException)
                {
                    return Response(new LocalMessage()
                        .WithAllowedMentions(LocalAllowedMentions.ExceptEveryone)
                        .WithContent($"You have private messages from the guild disabled, {Mention.User(Context.Author)}, please enable them and try again."));
                }
            }

            return Response(new LocalMessage()
                .WithAllowedMentions(LocalAllowedMentions.ExceptEveryone)
                .WithContent($"Check your DM's, {Mention.User(Context.Author)}"));
        }
        
        [Command]
        [Description("Retrieves help from a specific module or command.")]
        [Priority(-10)]
        public async Task HelpAsync(
            [Remainder] [Description("Name of the module or command to query.")]
                string query)
        {
            await HelpAsync(query, HelpDataType.Command | HelpDataType.Module);
        }

        [Command("module", "modules")]
        [Description("Retrieves help from a specific module. Useful for modules that have an overlapping command name.")]
        public async Task HelpModuleAsync(
            [Description("Name of the module to query.")][Remainder]
                string query)
        {
            await HelpAsync(query, HelpDataType.Module);
        }

        [Command("command", "commands")]
        [RequireAuthorGuildPermissions(Permission.Administrator)]
        [Description("Retrieves help from a specific command. Useful for commands that have an overlapping module name.")]
        public async Task HelpCommandAsync(
            [Description("Name of the module to query.")][Remainder]
            string query)
        {
            await HelpAsync(query, HelpDataType.Command);
        }
        private async Task HelpAsync(string query, HelpDataType type)
        {
            var sanitizedQuery = FormatUtilities.SanitizeAllMentions(query);

            if (TryGetEmbed(query, type, out var localEmbed))
            {
                await Context.Channel.SendMessageAsync(new LocalMessage().WithContent($"Results for \"{sanitizedQuery}\"").WithEmbeds(localEmbed));
                return;
            }

            await Context.Channel.SendMessageAsync(new LocalMessage().WithContent($"No results matching \"{query}\" were found."));
        }
        
        private bool TryGetEmbed(string query, HelpDataType type, out LocalEmbed embed)
        {
            embed = null;
            if (type.HasFlag(HelpDataType.Module))
            {
                var byModule = _commandService.GetAllModules().FirstOrDefault(x => x.Name.Equals(query, StringComparison.OrdinalIgnoreCase));
                if (byModule != null)
                {
                    embed = GetEmbedForModule(byModule);
                    return true;
                }
            }

            if (type.HasFlag(HelpDataType.Command))
            {
                var byCommand = _commandService.FindCommands(query);
                if (byCommand.Count != 0)
                {
                    embed = GetEmbedForCommand(byCommand[0].Command);
                    return true;
                }
            }

            return false;
        }

        private LocalEmbed GetEmbedForModule(Module module)
        {
            var localEmbed = new LocalEmbed()
                .WithTitle($"Module: {module.Name}")
                .WithDescription(module.Description);
            foreach (var command in module.Commands)
            {
                AddCommandFields(localEmbed, command);
            }

            return localEmbed;
        }
        private LocalEmbed GetEmbedForCommand(Command command)
        {
            return AddCommandFields(new LocalEmbed(), command);
        }

        private LocalEmbed AddCommandFields(LocalEmbed embed, Command command)
        {
            var summaryBuilder = new StringBuilder(command.Description ?? "No summary.").AppendLine();
            var name = command.Name;
            AppendRequiredClaims(summaryBuilder, command);
            AppendParameters(summaryBuilder, command.Parameters);
            AppendAliases(summaryBuilder, command.FullAliases.Where(x => !x.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList());

            embed.AddField(new LocalEmbedField()
                .WithName($"Command: {DoraemonConfig.Prefix}{name} {GetParams(command)}")
                .WithValue(summaryBuilder.ToString()));
            return embed;
        }

        private StringBuilder AppendAliases(StringBuilder stringBuilder, IReadOnlyCollection<string> commandAliases)
        {
            if (commandAliases.Count == 0)
            {
                return stringBuilder;
            }

            List<string> indexedCommandAliases = commandAliases.ToList();
            indexedCommandAliases.RemoveAt(0);
            stringBuilder.AppendLine($"**Aliases:**");
            foreach (var alias in FormatUtilities.CollapsePlurals(indexedCommandAliases))
            {
                if (string.IsNullOrEmpty(alias)) continue;
                stringBuilder.AppendLine($"• {alias}");
            }

            return stringBuilder;
        }
        private StringBuilder AppendParameters(StringBuilder stringBuilder, IReadOnlyList<Parameter> parameters)
        {
            if (parameters.Count == 0)
            {
                return stringBuilder;
            }

            stringBuilder.AppendLine($"**Parameters:**");
            foreach (var parameter in parameters)
            {
                if (!(parameter.Description is null))
                {
                    stringBuilder.AppendLine($"• **{parameter.Name}: {parameter.Description}**");
                }
            }

            return stringBuilder;
        }

        private StringBuilder AppendRequiredClaims(StringBuilder stringBuilder, Command command)
        {
            var claims = command.Attributes.OfType<RequireClaims>().FirstOrDefault();
            if (claims == null)
                return stringBuilder;
            stringBuilder.AppendLine($"**Required Claims**");
            var joinedClaims = string.Join(" ,", claims._claims);
            stringBuilder.AppendLine($"• **{joinedClaims}**");
            return stringBuilder;
        }
        private string GetParams(Command command)
        {
            var stringBuilder = new StringBuilder();
            foreach (var parameter in command.Parameters)
            {
                if (parameter.IsOptional)
                    stringBuilder.Append($"[{parameter.Name}]");
                else
                    stringBuilder.Append($"<{parameter.Name}>");
            }

            return stringBuilder.ToString();
        }
    }
    
}