using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Doraemon.Common.Extensions;
using Doraemon.Services.Core;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Qmmands;
using Serilog;

namespace Doraemon.Modules
{
    [Name("Eval")]
    [Description("Provides utilities to evaluate C-Sharp code.")]
    [Group("eval", "evaluate", "exec", "e")]
    public class CSharpEvalModule : DoraemonGuildModuleBase
    {
        [Command]
        [RunMode(RunMode.Parallel)]
        [Description("Evaluates the code provided.")]
        public async Task<DiscordCommandResult> EvalAsync(
            [Description("The code to evaluate.")] [Remainder]
                string code)
        {
            if (! await Context.Bot.IsOwnerAsync(Context.Author.Id)) return null;
                var scriptOptions = ScriptOptions.Default
                .WithImports(EvalService.EvalNamespaces)
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));
            code = EvalService.ValidateCode(code);
            var script = CSharpScript.Create(code, scriptOptions, Context is DiscordGuildCommandContext ? typeof(EvalGuildGlobals) : typeof(EvalGlobals));
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var diagnostics = script.Compile();
                stopwatch.Stop();
                if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                {
                    var compileErrorEmbed = new LocalEmbed()
                        .WithTitle("Compilation Failure")
                        .WithDescription($"{diagnostics.Length} {(diagnostics.Length > 1 ? "errors" : "error")}")
                        .WithColor(DColor.Red)
                        .WithFooter($"{stopwatch.Elapsed.TotalMilliseconds}ms");
                    for (var i = 0; i < diagnostics.Length; i++)
                    {
                        if (i > 3)
                            break;
                        var diagnostic = diagnostics[i];
                        var lineSpan = diagnostic.Location.GetLineSpan().Span;
                        compileErrorEmbed.AddField($"Error `{diagnostic.Id}` at {lineSpan.Start} - {lineSpan.End}", diagnostic.GetMessage());
                    }

                    return Response(compileErrorEmbed);
                }

                var globals = Context is DiscordGuildCommandContext guildContext ? new EvalGuildGlobals(guildContext) : new EvalGuildGlobals(Context);
                var state = await script.RunAsync(globals, _ => true);
                if (state.Exception != null)
                {
                    var runErrorEmbed = new LocalEmbed()
                        .WithTitle($"Runtime Failure")
                        .WithDescription(state.Exception.ToString().Truncate(LocalEmbed.MaxDescriptionLength))
                        .WithColor(DColor.Red)
                        .WithFooter($"{stopwatch.Elapsed.TotalMilliseconds} ms");
                    return Response(runErrorEmbed);
                }

                switch (state.ReturnValue)
                {
                    case null:
                    case string value when string.IsNullOrWhiteSpace(value):
                        return Reaction(new LocalEmoji("✅"));
                    case DiscordCommandResult commandResult:
                        return commandResult;
                    default:
                        return Response(state.ReturnValue.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Eval unexpectedly failed.");
                return Response($"An exception bubbled up: {ex.Message}");
            }
        }
    }
}