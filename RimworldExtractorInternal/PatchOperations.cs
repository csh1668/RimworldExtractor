using System.Xml;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal;

internal static class PatchOperations
{
    /// <summary>
    /// Defs added by Patch operations, to be extracted after Patch operations end.
    /// Item1) required mods, Item2) Xml Node
    /// </summary>
    public static readonly List<(List<string>?, XmlNode)> DefsAddedByPatches = new();

    public static IEnumerable<TranslationEntry> PatchOperationRecursive(XmlNode curNode, List<string>? requiredMods)
    {
        if (Extractor.CombinedDefs == null)
            yield break;

        var operation = curNode.Attributes?["Class"]?.Value;
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

    private static IEnumerable<TranslationEntry> PatchOperationInsert(XmlNode curNode, List<string>? requiredMods)
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath �Ǵ� value�� ���� �����ϴ�. �߸��� ������ XML ����.");
            yield break; // yield break;
        }

        var selectNodes = Extractor.CombinedDefs.SelectNodesSafe(xpath);
        if (selectNodes == null) yield break;
        foreach (XmlNode selectNode in selectNodes)
        {
            var parentNode = selectNode.ParentNode;
            if (parentNode == null)
            {
                Log.Wrn($"���õ� ��� {selectNode.Name}�� �θ� ��尡 �����ϴ�.");
            }
            var rootDefNode = Extractor.GetRootDefNode(parentNode, out var nodeName);
            foreach (XmlElement valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = parentNode.InsertAfter(Extractor.CombinedDefs!.ImportNode(valueChildNode, true), selectNode)!;
                rootDefNode ??= selectNodeImported;
                foreach (var translation in Extractor.FindExtractableNodes(rootDefNode["defName"]!.InnerText,
                             rootDefNode.Attributes?["Class"]?.Value ?? rootDefNode.Name, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods?.ToHashSet()
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationAddModExtension(XmlNode curNode, List<string>? requiredMods)
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath �Ǵ� value�� ���� �����ϴ�. �߸��� ������ XML ����.");
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
                foreach (var translation in Extractor.FindExtractableNodes(rootDefNode["defName"]!.InnerText,
                             rootDefNode.Attributes?["Class"]?.Value ?? rootDefNode.Name, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods?.ToHashSet()
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationReplace(XmlNode curNode, List<string>? requiredMods)
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath �Ǵ� value�� ���� �����ϴ�. �߸��� ������ XML ����.");
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
                Log.Wrn($"defName �Ǵ� className�� ã�� �� ���� Patch: xpath:{xpath}");
            foreach (XmlNode valueChildNode in value.ChildNodes)
            {
                XmlNode selectNodeImported = parentNode.InsertBefore(Extractor.CombinedDefs!.ImportNode(valueChildNode, true), selectNode)!;
                foreach (var translation in Extractor.FindExtractableNodes(defName, className, selectNodeImported, nodeName))
                {
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods?.ToHashSet()
                    };
                }
            }
            parentNode.RemoveChild(selectNode);
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationAdd(XmlNode curNode, List<string>? requiredMods) 
    {
        var xpath = curNode["xpath"]?.InnerText;
        XmlNode? value = curNode["value"];
        if (xpath == null || value == null)
        {
            Log.Wrn($"xpath �Ǵ� value�� ���� �����ϴ�. �߸��� ������ XML ����.");
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
                rootDefNode ??= selectNodeImported;

                if (xpath is "Defs" or "Defs/")
                {
                    DefsAddedByPatches.Add((requiredMods, selectNodeImported));
                    continue;
                }

                var defName = rootDefNode["defName"]?.InnerText;
                if (defName == null)
                {

                    Log.Wrn($"defName�� ���� ���� �������� �ʽ��ϴ�. xpath={xpath}, value={value.InnerXml}");
                    continue;
                }

                foreach (var translation in Extractor.FindExtractableNodes(defName,
                             rootDefNode.Attributes?["Class"]?.Value ?? rootDefNode.Name, selectNodeImported, nodeName))
                {
                    if (translation.ClassName == "Keyed")
                    {
                        yield return translation with
                        {
                            RequiredMods = requiredMods?.ToHashSet()
                        };
                        continue;
                    }
                    yield return translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods?.ToHashSet()
                    };
                }
            }
        }
    }

    private static IEnumerable<TranslationEntry> PatchOperationSequence(XmlNode curNode, List<string>? requiredMods)
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

    private static IEnumerable<TranslationEntry> PatchOperationFindMod(XmlNode curNode, List<string>? requiredMods)
    {
        requiredMods ??= new List<string>();
        var prevRequiredMods = new List<string>(requiredMods);
        var requiredModNodes = curNode["mods"]?.ChildNodes;
        if (requiredModNodes != null)
        {
            requiredMods.AddRange(from XmlNode requiredModsNode in requiredModNodes select requiredModsNode.InnerText);
        }

        var match = curNode["match"];
        if (match != null)
        {
            foreach (var translationEntry in PatchOperationRecursive(match, requiredMods))
            {
                yield return translationEntry;
            }
        }

        var noMatch = curNode["nomatch"];
        if (noMatch != null)
        {
            foreach (var translationEntry in PatchOperationRecursive(noMatch, prevRequiredMods))
            {
                yield return translationEntry;
            }
        }
    }
}