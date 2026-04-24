using System.CommandLine;

namespace RimworldExtractor.Cli.Commands;

/// <summary>
/// rimextract analyze --mod &lt;name&gt; --against &lt;file.xlsx&gt;
///
/// Deferred: full implementation (diff current extraction vs stored XLSX) is not
/// included in v2.0. Returns exit code 2 with a message directing users to the GUI.
/// Implementation plan: Phase 7.
/// </summary>
internal sealed class AnalyzeCommand : Command
{
    private readonly Option<string?> _modOption = new("--mod")
    {
        Description = "Mod name or packageId to analyze.",
    };

    private readonly Option<string?> _againstOption = new("--against")
    {
        Description = "Baseline .xlsx file to compare against.",
    };

    // configOption kept for API consistency; not used in this stub.
    public AnalyzeCommand(Option<string?> configOption) : base("analyze", "Compare current extraction against a baseline XLSX (not implemented in v2.0; use GUI).")
    {
        _ = configOption; // deferred — silence unused-variable warning

        Add(_modOption);
        Add(_againstOption);

        SetAction(Handle);
    }

    private static int Handle(ParseResult _)
    {
        Console.Error.WriteLine("analyze: not implemented in v2.0 — use the GUI for comparison workflows.");
        Console.Error.WriteLine("Full diff support is planned for v2.1 / Phase 7.");
        return 2;
    }
}
