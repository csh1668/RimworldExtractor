using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal.Compats
{
    internal class Compat_MVCF : BaseCompat
    {
        private const string verbPropsKeyword = "Comp_VerbProps.verbProps";
        private const string verbKeyword = ".verbs.";
        public override IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            var translations = entries.ToList();

            HashSet<MVCFForm> mvcfForms = new HashSet<MVCFForm>();
            // CAT_MonoWhipClawBladelink.comps.Comp_VerbProps.verbProps.0.label
            // Find 
            foreach (var entry in translations.Where(x => x.Node.Contains(verbPropsKeyword)))
            {
                var tokens = entry.Node.Split('.');
                var defName = tokens[0];
                var lastToken = tokens.Last();

                if (lastToken == "label")
                {
                    mvcfForms.Add(new MVCFForm(defName, entry));
                }
            }

            foreach (var entry in translations.Where(x => x.Node.Contains(verbPropsKeyword)))
            {
                var tokens = entry.Node.Split('.');
                var defName = tokens[0];
                var lastToken = tokens.Last();

                if (lastToken == "visualLabel")
                {
                    tokens[^1] = "label";
                    var mvcfForm = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel.Node == string.Join('.', tokens));
                    if (mvcfForm != null)
                    {
                        mvcfForm.VerbPropsVisualLabel = entry;
                    }
                }

                if (lastToken == "description")
                {
                    tokens[^1] = "label";
                    var mvcfForm = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel.Node == string.Join('.', tokens));
                    if (mvcfForm != null)
                    {
                        mvcfForm.VerbPropsDescription = entry;
                    }
                }
            }

            foreach (var entry in translations.Where(x => x.Node.Contains(verbKeyword)))
            {
                var tokens = entry.Node.Split('.');
                var defName = tokens[0];
                var lastToken = tokens.Last();

                if (lastToken == "label")
                {
                    var value = entry.Original;
                    var mvcfForm = mvcfForms.FirstOrDefault(x => x.DefName == defName && x.VerbPropsLabel.Original == value);
                    if (mvcfForm != null)
                    {
                        mvcfForm.VerbLabel = entry;
                    }
                }
            }

            foreach (var entry in translations)
            {
                var mvcfForm = mvcfForms.FirstOrDefault(x => x.VerbPropsLabel == entry);
                if (mvcfForm != null)
                {
                    if (mvcfForm.VerbPropsVisualLabel == null)
                    {
                        var tokens = mvcfForm.VerbPropsLabel.Node.Split('.');
                        tokens[^1] = "visualLabel";

                        yield return mvcfForm.VerbPropsLabel with
                        {
                            Node = string.Join('.', tokens)
                        };
                    }
                    else
                    {
                        yield return mvcfForm.VerbPropsVisualLabel;
                    }

                    if (mvcfForm.VerbPropsDescription == null)
                    {
                        var tokens = mvcfForm.VerbPropsLabel.Node.Split('.');
                        tokens[^1] = "description";

                        var verbPropsDescription = (new TranslationEntry(mvcfForm.VerbPropsLabel) with
                        {
                            Node = string.Join('.', tokens),
                            Original = ""
                        });
                        verbPropsDescription.AddExtension(Prefabs.ExtensionKeyExtraCommentTranslated, "해당 노드를 번역하면 gizmo에 표시되는 설명을 수정할 수 있습니다");
                        yield return verbPropsDescription;
                    }
                    else
                    {
                        yield return mvcfForm.VerbPropsDescription;
                    }
                }

                if (mvcfForms.Any(x => x.VerbPropsLabel == entry || x.VerbLabel == entry || x.VerbPropsVisualLabel == entry))
                    continue;
                yield return entry;
            }
        }

        private class MVCFForm
        {
            public readonly string DefName;
            public readonly TranslationEntry VerbPropsLabel;
            public TranslationEntry? VerbPropsVisualLabel = null;
            public TranslationEntry? VerbLabel = null;
            public TranslationEntry? VerbPropsDescription = null;

            public MVCFForm(string defName, TranslationEntry verbPropsLabel)
            {
                DefName = defName;
                VerbPropsLabel = verbPropsLabel;
            }
        }
    }
}
