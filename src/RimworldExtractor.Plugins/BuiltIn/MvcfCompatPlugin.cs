using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// MVCF (Multiple Verb Combat Framework) compat plugin.
/// Synthesizes <c>visualLabel</c> and <c>description</c> entries from
/// <c>Comp_VerbProps.verbProps.*.label</c> entries, then filters out the
/// intermediate entries that are no longer needed.
///
/// Ports <c>legacy/Compats/Compat_MVCF.cs</c>.
/// Priority: 100 (default).
///
/// TODO: The legacy code emits an extension comment
/// ("해당 노드를 번역하면 gizmo에 표시되는 설명을 수정할 수 있습니다") on synthesized
/// description entries via <c>TranslationEntry.AddExtension</c>. Phase 2 dropped
/// <c>AddExtension</c> (YAGNI). The comment is cosmetic (UI-only) and does not affect
/// Original/Translated/Node/ClassName — skipped for Phase 4 MVP.
/// </summary>
public sealed class MvcfCompatPlugin : CompatPluginBase
{
    private const string VerbPropsKeyword = "Comp_VerbProps.verbProps";
    private const string VerbKeyword = ".verbs.";

    public override string Name => "MVCF";

    public override IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
    {
        var translations = entries.ToList();

        var mvcfForms = new HashSet<MvcfForm>();

        // Pass 1: find all VerbPropsLabel entries (contain verbPropsKeyword and end in ".label")
        foreach (var entry in translations.Where(x => x.Node.Contains(VerbPropsKeyword)))
        {
            var tokens = entry.Node.Split('.');
            if (tokens[^1] == "label")
            {
                mvcfForms.Add(new MvcfForm(tokens[0], entry));
            }
        }

        // Pass 2: find visualLabel and description entries for each form
        foreach (var entry in translations.Where(x => x.Node.Contains(VerbPropsKeyword)))
        {
            var tokens = entry.Node.Split('.');
            var lastToken = tokens[^1];

            if (lastToken == "visualLabel")
            {
                tokens[^1] = "label";
                var form = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel.Node == string.Join('.', tokens));
                if (form is not null)
                    form.VerbPropsVisualLabel = entry;
            }
            else if (lastToken == "description")
            {
                tokens[^1] = "label";
                var form = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel.Node == string.Join('.', tokens));
                if (form is not null)
                    form.VerbPropsDescription = entry;
            }
        }

        // Pass 3: find matching VerbLabel entries (entries ending in ".label" that are under .verbs.)
        foreach (var entry in translations.Where(x => x.Node.Contains(VerbKeyword)))
        {
            var tokens = entry.Node.Split('.');
            if (tokens[^1] != "label") continue;

            var defName = tokens[0];
            var form = mvcfForms.FirstOrDefault(x => x.DefName == defName && x.VerbPropsLabel.Original == entry.Original);
            if (form is not null)
                form.VerbLabel = entry;
        }

        // Output pass: synthesize or pass-through
        foreach (var entry in translations)
        {
            var form = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel == entry);
            if (form is not null)
            {
                // Synthesize visualLabel
                if (form.VerbPropsVisualLabel is null)
                {
                    var tokens = form.VerbPropsLabel.Node.Split('.');
                    tokens[^1] = "visualLabel";
                    yield return form.VerbPropsLabel with { Node = string.Join('.', tokens) };
                }
                else
                {
                    yield return form.VerbPropsVisualLabel;
                }

                // Synthesize description (extension comment omitted — Phase 4 MVP, see TODO above)
                if (form.VerbPropsDescription is null)
                {
                    var tokens = form.VerbPropsLabel.Node.Split('.');
                    tokens[^1] = "description";
                    yield return form.VerbPropsLabel with
                    {
                        Node = string.Join('.', tokens),
                        Original = string.Empty
                    };
                }
                else
                {
                    yield return form.VerbPropsDescription;
                }
            }

            // Skip entries that are part of the MVCF form (they're replaced by synthesized ones)
            if (mvcfForms.Any(x => x.VerbPropsLabel == entry || x.VerbLabel == entry || x.VerbPropsVisualLabel == entry))
                continue;

            yield return entry;
        }
    }

    private sealed class MvcfForm
    {
        public readonly string DefName;
        public readonly TranslationEntry VerbPropsLabel;
        public TranslationEntry? VerbPropsVisualLabel;
        public TranslationEntry? VerbLabel;
        public TranslationEntry? VerbPropsDescription;

        public MvcfForm(string defName, TranslationEntry verbPropsLabel)
        {
            DefName = defName;
            VerbPropsLabel = verbPropsLabel;
        }
    }
}
