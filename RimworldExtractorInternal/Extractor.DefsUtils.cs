using RimworldExtractorInternal.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Text.RegularExpressions;

namespace RimworldExtractorInternal
{
    public static partial class Extractor
    {
        public static void LoadReferenceDefs(List<string> referenceDefsRoots)
        {
            if (CombinedDefs == null)
            {
                Log.Err($"{nameof(CombinedDefs)} should have to be non-null but is null.");
                return;
            }

            foreach (var referenceDefsRoot in referenceDefsRoots)
            {
                foreach (var filePath in IO.DescendantFiles(referenceDefsRoot)
                             .Where(x => x.ToLower().EndsWith(".xml")))
                {
                    try
                    {
                        var childDoc = IO.ReadXml(filePath);

                        foreach (XmlNode node in childDoc.DocumentElement!.ChildNodes)
                        {
                            var newNode = CombinedDefs.ImportNode(node, true);
                            var newAttribute = CombinedDefs.CreateAttribute("Reference");
                            newAttribute.Value = "True";
                            newNode.Attributes?.Append(newAttribute);
                            CombinedDefs.DocumentElement!.AppendChild(newNode);
                            var attributeName = node.Attributes?["Name"]?.Value;
                            if (attributeName != null)
                            {
                                if (ParentNodeLookUp.ContainsKey(attributeName))
                                {
                                    Log.Wrn($"Parent 노드의 이름이 겹칩니다: {attributeName}. 나중 것으로 덮어씌웁니다.");
                                }
                                ParentNodeLookUp[attributeName] = newNode;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Err($"{filePath} 를 읽는 중 에러 발생, {e.Message}");
                        throw;
                    }
                }
            }
        }

        internal static IEnumerable<TranslationEntry> FindExtractableNodes(string defName, string className, XmlNode rootNode, string? curNormalizedPath = null)
        {
            if (className == "XmlExtensions.SettingsMenuDef")
            {
                foreach (var translationEntry in FindExtractableNodesXmlExtensionSettings(defName, className, rootNode, curNormalizedPath))
                {
                    yield return translationEntry;
                }
                yield break;
            }

            var requiredMods = rootNode["REQUIREDMODS"]?.ChildNodes.OfType<XmlNode>().Select(x => x.InnerText).ToHashSet();

            // (CurrentNode, CurrentPath)
            var q = new Queue<(XmlNode, string)>();

            if (curNormalizedPath == null)
            {
                foreach (XmlNode node in rootNode.ChildNodes)
                {
                    q.Enqueue((node, node.IsListNode() ? GetIdxOfListNode(node).ToString() : node.Name));
                }
            }
            else
            {
                q.Enqueue((rootNode, curNormalizedPath + "." + (rootNode.IsListNode() 
                    ? GetIdxOfListNode(rootNode).ToString() : rootNode.Name)));
            }

            while (q.Count > 0)
            {
                var (curNode, curPath) = q.Dequeue();

                var token = curPath.Split('.');
                var lastTag = token[^1];

                if (curNode.IsTextNode())
                {
                    var isListNode = token.Length > 1 && int.TryParse(lastTag, out _) &&
                                     Prefabs.ExtractableTags.Contains(token[^2]);
                    if (Prefabs.ExtractableTags.Contains(lastTag) || isListNode)
                    {
                        var nodeName = $"{defName}.{curPath}";
                        if (curNormalizedPath != null)
                            nodeName = curPath;
                        else if (curNode.Attributes?["TKey"]?.Value != null)
                        {
                            var tKey = curNode.Attributes["TKey"]!.Value;
                            nodeName = $"{defName}.{tKey}.slateRef";
                        }
                        var translation = new TranslationEntry(className, nodeName, curNode.InnerText, null, requiredMods);
                        yield return translation;
                    }
                    continue;
                }

                foreach (XmlNode childNode in curNode.ChildNodes)
                {
                    string path;
                    if (childNode.IsListNode())
                    {
                        if (MatchTranslationHandle(childNode, out var translationHandleValue))
                            path = $"{curPath}.{translationHandleValue}";
                        else
                            path = $"{curPath}.{GetIdxOfListNode(childNode)}";
                    }
                    else
                        path = $"{curPath}.{childNode.Name}";


                    q.Enqueue((childNode, path));
                }
            }
        }

        private static IEnumerable<TranslationEntry> FindExtractableNodesXmlExtensionSettings(string defName, string className,
            XmlNode rootNode, string? curNormalizedPath = null)
        {
            var extractableTagsXmlExtensionSettings = new[] { "label", "text", "tooltip" };

            // (CurrentNode, CurrentPath)
            var q = new Queue<(XmlNode, string)>();

            if (curNormalizedPath == null)
            {
                foreach (XmlNode node in rootNode.ChildNodes)
                {
                    q.Enqueue((node, node.IsListNode() ? GetIdxOfListNode(node).ToString() : node.Name));
                }
            }
            else
            {
                q.Enqueue((rootNode, curNormalizedPath + "." + (rootNode.IsListNode()
                    ? GetIdxOfListNode(rootNode).ToString() : rootNode.Name)));
            }

            while (q.Count > 0)
            {
                var (curNode, curPath) = q.Dequeue();

                var token = curPath.Split('.');
                var lastTag = token[^1];

                if (curNode.IsTextNode())
                {
                    var isListNode = token.Length > 1 && int.TryParse(lastTag, out _) &&
                                     extractableTagsXmlExtensionSettings.Contains(token[^2]);
                    if (extractableTagsXmlExtensionSettings.Contains(lastTag) || isListNode)
                    {
                        var tKey = curNode.ParentNode?["tKey"]?.InnerText;
                        var tKeyTip = curNode.ParentNode?["tKeyTip"]?.InnerText;
                        if (lastTag is "label" or "text" && tKey != null)
                        {
                            yield return new TranslationEntry("Keyed", tKey, curNode.InnerText, null);
                        }
                        else if (lastTag == "tooltip" && tKeyTip != null)
                        {
                            yield return new TranslationEntry("Keyed", tKeyTip, curNode.InnerText, null);
                        }
                        else
                        {
                            yield return new TranslationEntry(className, $"{defName}.{curPath}", curNode.InnerText, null);
                        }
                    }

                    continue;
                }

                foreach (XmlNode childNode in curNode.ChildNodes)
                {
                    string path;
                    if (childNode.IsListNode())
                    {
                        //if (MatchTranslationHandle(childNode, out var translationHandleResult))
                        //    path = $"{curPath}.{translationHandleResult}";
                        //else
                        path = $"{curPath}.{GetIdxOfListNode(childNode)}";
                    }
                    else
                        path = $"{curPath}.{childNode.Name}";

                    q.Enqueue((childNode, path));
                }
            }
        }

        private static bool MatchTranslationHandle(XmlNode node, out string translationHandleResult)
        {
            translationHandleResult = string.Empty;
            if (!node.HasChildNodes)
                return false;
            foreach (var handle in Prefabs.TranslationHandles)
            {
                var isTypeField = handle.StartsWith('*');
                var translationHandleMatcher = isTypeField ? handle[1..] : handle;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    var name = childNode.Name;
                    if (childNode.ChildNodes.Count == 1 && childNode.FirstChild!.NodeType == XmlNodeType.Text && translationHandleMatcher == name)
                    {
                        translationHandleResult = isTypeField ? 
                            childNode.InnerText.Split('.').Last() : NormalizedHandle(childNode.InnerText);
                        if (string.IsNullOrWhiteSpace(translationHandleResult))
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        private static string NormalizedHandle(string handle)
        {
            if (string.IsNullOrEmpty(handle))
            {
                return handle;
            }
            handle = handle.Trim();
            handle = handle.Replace(' ', '_');
            handle = handle.Replace('\n', '_');
            handle = handle.Replace("\r", "");
            handle = handle.Replace('\t', '_');
            handle = handle.Replace(".", "");
            if (handle.IndexOf('-') >= 0)
            {
                handle = handle.Replace('-'.ToString(), "");
            }
            if (handle.IndexOf('{') >= 0)
            {
                handle = new Regex("{.*?}").Replace(handle, "");
            }

            var sb = new StringBuilder();
            for (int i = 0; i < handle.Length; i++)
            {
                if ("qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890-_".IndexOf(handle[i]) >= 0)
                {
                    sb.Append(handle[i]);
                }
            }
            handle = sb.ToString();
            sb.Length = 0;
            for (int j = 0; j < handle.Length; j++)
            {
                if (j == 0 || handle[j] != '_' || handle[j - 1] != '_')
                {
                    sb.Append(handle[j]);
                }
            }
            handle = sb.ToString();
            handle = handle.Trim(new char[]
            {
                '_'
            });
            if (!string.IsNullOrEmpty(handle) && handle.All(char.IsDigit))
            {
                handle = "_" + handle;
            }
            return handle;
        }


        private static void DoXmlInheritance(IEnumerable<XmlNode>? customNodes = null)
        {
            if (CombinedDefs == null)
            {
                return;
            }

            var newDoc = new XmlDocument();
            newDoc.AppendChild(newDoc.CreateElement("Defs"));

            customNodes ??= CombinedDefs.DocumentElement!.ChildNodes.OfType<XmlNode>().ToList();

            foreach (XmlNode node in customNodes)
            {
                if (node.Attributes == null || node.Attributes["Abstract"]?.Value.ToLower() == "true"
                    // || node.Attributes["Reference"]?.Value.ToLower() == "true"
                    )
                    continue;

                var parentName = node.Attributes["ParentName"]?.Value;
                if (parentName == null)
                {
                    newDoc.DocumentElement!.AppendChild(newDoc.ImportNode(node, true));
                    continue;
                }
                Stack<XmlNode> parentNodes = new Stack<XmlNode>();
                parentNodes.Push(node);
                while (true)
                {
                    if (parentName != null)
                    {
                        if (ParentNodeLookUp.TryGetValue(parentName, out var parentNode))
                        {
                            parentNodes.Push(parentNode);
                            parentName = parentNode?.Attributes?["ParentName"]?.Value;
                        }
                        else
                        {
                            Log.Wrn($"자식 노드={node["defName"]?.InnerText ?? "UNKNOWN"}의 부모 노드={parentName}를 찾을 수 없었습니다. ");
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                var mergedNode = newDoc.ImportNode(node, true);
                while (mergedNode.FirstChild != null)
                {
                    mergedNode.RemoveChild(mergedNode.FirstChild);
                }
                mergedNode.Attributes?.RemoveNamedItem("ParentName");
                while (parentNodes.Count > 0)
                {
                    var parentNode = parentNodes.Pop();
                    XmlOverwriteRecursive(mergedNode, parentNode);
                }

                var requiredPackageId = node.Attributes?["RequiredPackageId"]?.Value;
                if (requiredPackageId != null)
                {
                    var attribute = mergedNode.Attributes.Append(newDoc.CreateAttribute("RequiredPackageId"));
                    attribute.Value = requiredPackageId;
                }

                if (node.Attributes["Reference"]?.Value.ToLower() == "true")
                {
                    var attribute = mergedNode.Attributes.Append(newDoc.CreateAttribute("Reference"));
                    attribute.Value = "True";
                }

                newDoc.DocumentElement!.AppendChild(mergedNode);
            }

            CombinedDefs = newDoc;
        }

        private static void XmlOverwriteRecursive(XmlNode original, XmlNode other)
        {
            if (original.Name != other.Name)
            {
                // Log.Wrn($"Different name, Original={Original.Name}|{Original.OuterXml}, other={other.Name}|{other.OuterXml}");
                return;
            }

            foreach (XmlNode otherChildNode in other.ChildNodes)
            {
                var existingChildNode = original[otherChildNode.Name];
                if (existingChildNode == null)
                {
                    original.AppendChild(original.OwnerDocument!.ImportNode(otherChildNode, true));
                    continue;
                }
                // 이미 존재할 경우
                // 1. 텍스트 노드 하나일 경우
                if (existingChildNode.IsTextNode())
                {
                    original.RemoveChild(existingChildNode);
                    original.AppendChild(original.OwnerDocument!.ImportNode(otherChildNode, true));
                    continue;
                }
                // 2. 리스트 노드일 경우
                else if (existingChildNode.FirstChild.IsListNode())
                {
                    foreach (XmlNode childNode in otherChildNode.ChildNodes)
                    {
                        existingChildNode.AppendChild(original.OwnerDocument!.ImportNode(childNode, true));
                    }

                    continue;
                }
                XmlOverwriteRecursive(existingChildNode, otherChildNode);
            }
        }

        internal static XmlNode? GetRootDefNode(XmlNode node, out string? nodeName)
        {
            if (node["defName"] != null)
            {
                nodeName = node["defName"]!.InnerText;
                return node;
            }
            else if (node.Name == "Defs")
            {
                nodeName = null;
                return null;
            }

            var parentNode = node;
            nodeName = node.IsListNode() ? GetIdxOfListNode(node).ToString() : node.Name;
            do
            {
                parentNode = parentNode.ParentNode;
                if (parentNode == null)
                    throw new InvalidOperationException("Couldn't find root Def node");
                if (parentNode["defName"] != null)
                {
                    nodeName = $"{parentNode["defName"]!.InnerText}.{nodeName}";
                    break;
                }
                nodeName = $"{(parentNode.IsListNode() ? GetIdxOfListNode(parentNode).ToString() : parentNode.Name)}.{nodeName}";
            } while (true);
            return parentNode;
        }

        private static int GetIdxOfListNode(XmlNode curNode)
        {
            var nodes = curNode.ParentNode?.ChildNodes;
            if (nodes == null)
                throw new InvalidOperationException("ParentNode was null.");
            int i;
            for (i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == curNode)
                    return i;
            }

            throw new InvalidOperationException("Couldn't find idx of list node.");
        }

    }
}
