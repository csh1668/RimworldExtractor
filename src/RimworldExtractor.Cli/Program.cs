using System.CommandLine;
using RimworldExtractor.Cli.Commands;

// Global option: --config <path> (recursive → accessible in all subcommands)
var configOption = new Option<string?>("--config")
{
    Description = "Path to settings.json (defaults to platform user-config dir).",
    Recursive = true,
};

var root = new RootCommand("RimWorld translation extractor — extract, convert, and analyze mod translations.");
root.Add(configOption);

root.Add(new ExtractCommand(configOption));
root.Add(new ConvertCommand(configOption));
root.Add(new AnalyzeCommand(configOption));

var config = new CommandLineConfiguration(root);
return await config.InvokeAsync(args);
