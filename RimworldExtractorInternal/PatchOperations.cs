using System.Xml;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal;

internal static class PatchOperations
{
    /// <summary>
    /// Defs added by Patch operations, to be extracted after Patch operations end.
    /// Item1) required mods, Item2) Xml Node
    /// </summary>
    public static readonly List<(RequiredMods?, XmlNode)> DefsAddedByPatches = new();

    public static IEnumerable<TranslationEntry> PatchOperationRecursive(XmlNode curNode, RequiredMods? requiredMods)
    {
        if (Extractor.CombinedDefs == null)
            yield break;

        var operation = curNode.Attributes?["Class"]?.Value;

        if (curNode.TryGetAttritube("MayRequire", out string? mayRequire) && mayRequire != null)
        {
            requiredMods = requiredMods != null ? new RequiredMods(requiredMods) : new RequiredMods();
            requiredMods.AddAllowedByPackageIds(mayRequire.Split(','));
        }

        XmlNode? success = curNode["success"];
        switch (operation)
        {
            case "PatchOperationFindMod":
                foreach (var translationEntry in PatchOperationFindMod(curNode, requiredMods))
                    yield return translationEntry;
                break;
            case "PatchOperationSequence":
                foreach (var translationEntry in PatchOperationSequence(curNode, requiredMods)) 
                    yield return translationEntry;
                break;
            case "PatchOperationAdd":
                foreach (var translationEntry in PatchOperationAdd(curNode, requiredMods)) 
                    yield return translationEntry;
                break;
            case "PatchOperationReplace":
                foreach (var translationEntry in PatchOperationReplace(curNode, requiredMods))
                    yield return translationEntry;
                break;
            case "PatchOperationAddModExtension":
                foreach (var translationEntry in PatchOperationAddModExtension(curNode, requiredMods))
                    yield return translationEntry;
                break;
            case "PatchOperationInsert":
                foreach (var translationEntry in PatchOperationInsert(curNode, requiredMods))
                    yield return translationEntry;
                break;
        }

        yield break;
    }

    private static IEnumerable<TranslationEntry> PatchOperationInsert(XmlNode curNode, RequiredMods? requiredMods)
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath 또는 value의 값이 없습니다. 잘못된 림월드 XML 포맷.");
            yield break; // yield break;
        }

        var selectNodes = Extractor.CombinedDefs.SelectNodesSafe(xpath);
        if (selectNodes == null) yield break;
        foreach (XmlNode selectNode in selectNodes)
        {
            var parentNode = selectNode.ParentNode;
            if (parentNode == null)
            {
                Log.Wrn($"선택된 노드 {selectNode.Name}의 부모 노드가 없습니다.");
                continue;
            }
            var rootDefNode = Extractor.GetRootDefNode(parentNode, out var nodeName);
            foreach (XmlElement valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = parentNode.InsertAfter(Extractor.CombinedDefs!.ImportNode(valueChildNode, true), selectNode)!;
                var curRootDefNode = rootDefNode ?? selectNodeImported;
                foreach (var translation in Extractor.FindExtractableNodes(curRootDefNode["defName"]!.InnerText,
                             curRootDefNode.Attributes?["Class"]?.Value ?? curRootDefNode.Name, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationAddModExtension(XmlNode curNode, RequiredMods? requiredMods)
    {
        if (Extractor.CombinedDefs == null) yield break;

        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath 또는 value의 값이 없습니다. 잘못된 림월드 XML 포맷.");
            yield break; // yield break;
        }

        var selectNodes = Extractor.CombinedDefs.SelectNodesSafe(xpath);
        if (selectNodes == null) yield break;
        foreach (XmlElement selectNode in selectNodes)
        {
            var rootDefNode = Extractor.GetRootDefNode(selectNode, out var nodeName);
            var modExtensionNode = selectNode["modExtensions"];
            if (modExtensionNode == null)
            {
                modExtensionNode = Extractor.CombinedDefs.CreateElement("modExtensions");
                selectNode.AppendChild(modExtensionNode);
                // XmlNode selectNodeImported = selectNode.AppendChild(CombinedDefs.ImportNode())
            }

            foreach (XmlNode valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = modExtensionNode.AppendChild(Extractor.CombinedDefs!.ImportNode(valueChildNode, true))!;
                var curRootDefNode = rootDefNode ?? selectNodeImported;
                foreach (var translation in Extractor.FindExtractableNodes(curRootDefNode["defName"]!.InnerText,
                             curRootDefNode.Attributes?["Class"]?.Value ?? curRootDefNode.Name, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationReplace(XmlNode curNode, RequiredMods? requiredMods)
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath 또는 value의 값이 없습니다. 잘못된 림월드 XML 포맷.");
            yield break; // yield break;
        }

        var selectNodes = Extractor.CombinedDefs.SelectNodesSafe(xpath);
        if (selectNodes == null) yield break;
        foreach (XmlNode selectNode in selectNodes)
        {
            var parentNode = selectNode.ParentNode!;
            var rootDefNode = Extractor.GetRootDefNode(parentNode, out var nodeName);
            var defName = rootDefNode?["defName"]?.InnerText;
            var className = (rootDefNode?.Attributes?["Class"]?.Value ?? rootDefNode?.Name);
                            
            if (rootDefNode == null)
            {
                defName = selectNode?["defName"]?.InnerText;
                className = (selectNode?.Attributes?["Class"]?.Value ?? selectNode?.Name);
            }
            if (defName is null || className is null)
                Log.Wrn($"defName 또는 className을 찾을 수 없는 Patch: xpath:{xpath}");
            foreach (XmlNode valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = parentNode.InsertBefore(Extractor.CombinedDefs!.ImportNode(valueChildNode, true), selectNode)!;
                foreach (var translation in Extractor.FindExtractableNodes(defName, className, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    };
                }
            }
            parentNode.RemoveChild(selectNode);
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationAdd(XmlNode curNode, RequiredMods? requiredMods) 
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath 또는 value의 값이 없습니다. 잘못된 림월드 XML 포맷.");
            yield break; // yield break;
        }

        var selectNodes = Extractor.CombinedDefs.SelectNodesSafe(xpath);
        if (selectNodes == null) yield break;
        foreach (XmlNode selectNode in selectNodes)
        {
            var rootDefNode = Extractor.GetRootDefNode(selectNode, out var nodeName);
            foreach (XmlNode valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = selectNode.AppendChild(Extractor.CombinedDefs!.ImportNode(valueChildNode, true))!;
                var curRootDefNode = rootDefNode ?? selectNodeImported;

                if (xpath is "Defs" or "Defs/")
                {
                    DefsAddedByPatches.Add((requiredMods, selectNodeImported));
                    continue;
                }

                var defName = curRootDefNode["defName"]?.InnerText;
                if (defName == null)
                {

                    Log.Wrn($"defName이 없는 경우는 지원하지 않습니다. xpath={xpath}, value={value.InnerXml}");
                    continue;
                }

                foreach (var translation in Extractor.FindExtractableNodes(defName,
                             curRootDefNode.Attributes?["Class"]?.Value ?? curRootDefNode.Name, selectNodeImported, nodeName))
                {
                    if (translation.ClassName == "Keyed")
                    {
                        yield return translation with
                        {
                            RequiredMods = requiredMods
                        };
                        continue;
                    }
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationSequence(XmlNode curNode, RequiredMods? requiredMods)
    {
        var operations = curNode["operations"];
        if (operations == null)
            yield break;
        foreach (XmlNode childOperation in operations.ChildNodes)
        {
            foreach (var translationEntry in PatchOperationRecursive(childOperation, requiredMods))
            {
                yield return translationEntry;
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationFindMod(XmlNode curNode, RequiredMods? requiredMods)
    {
        requiredMods = new RequiredMods(requiredMods);
        var noMatchRequiredMods = new RequiredMods(requiredMods);
        var requiredModNodes = curNode["mods"]?.ChildNodes;
        var requiredModsList = requiredModNodes?.Select(n => n.InnerText).ToList();

        var match = curNode["match"];
        if (match != null)
        {
            if (requiredModsList != null)
            {
                requiredMods.AddAllowedByModNames(requiredModsList);
            }
            foreach (var translationEntry in PatchOperationRecursive(match, requiredMods))
            {
                yield return translationEntry;
            }
        }

        var noMatch = curNode["nomatch"];
        if (noMatch != null)
        {
            if (requiredModsList != null)
            {
                noMatchRequiredMods.AddDisallowedByModNames(requiredModsList);
            }
            foreach (var translationEntry in PatchOperationRecursive(noMatch, noMatchRequiredMods))
            {
                yield return translationEntry;
            }
        }
    }
}