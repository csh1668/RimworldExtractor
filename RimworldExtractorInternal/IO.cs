﻿using ClosedXML.Excel;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;
using RimworldExtractorInternal.Compats;
using RimworldExtractorInternal.DataTypes;
using RimworldExtractorInternal.Exceptions;

namespace RimworldExtractorInternal
{
    public static class IO
    {
        private static readonly string HeaderClassNode = "Class+Node [(Identifier (Key)]";
        private static readonly string HeaderClass = "Class [Not chosen]";
        private static readonly string HeaderNode = "Node [Not chosen]";
        private static readonly string HeaderRequiredMods = "Required Mods [Not chosen]";
        private static string HeaderOriginal => $"{Prefabs.OriginalLanguage} [Source string]";
        private static string HeaderTranslated => $"{Prefabs.TranslationLanguage} [Translation]";
        public static void ToExcel(List<TranslationEntry> translations, string outPath = "result")
        {
            var xlsx = new XLWorkbook();
            var sheet = xlsx.AddWorksheet();
            sheet.Cell(1, 1).Value = HeaderClassNode;
            sheet.Cell(1, 2).Value = HeaderClass;
            sheet.Cell(1, 3).Value = HeaderNode;
            sheet.Cell(1, 4).Value = HeaderRequiredMods;
            sheet.Cell(1, 5).Value = HeaderOriginal;
            sheet.Cell(1, 6).Value = HeaderTranslated;
            for (int i = 0; i < translations.Count; i++)
            {
                var entry = translations[i];
                sheet.Cell(2 + i, 1).Value = $"{entry.ClassName}+{entry.Node}";
                sheet.Cell(2 + i, 2).Value = entry.ClassName;
                sheet.Cell(2 + i, 3).Value = entry.Node;
                if (entry.RequiredMods != null)
                {
                    var combinedRequiredMods = entry.RequiredMods.ToString();
                    sheet.Cell(2 + i, 4).Value = combinedRequiredMods;
                    if (combinedRequiredMods.Contains("##packageId##") && entry.ClassName.StartsWith("Patches."))
                    {
                        Log.WrnOnce($"Required Mods 열에 잘못된 값이 존재합니다. 추후 Patches의 올바른 생성을 위해 엑셀 파일에 있는 해당 문구: \"{combinedRequiredMods}\" 를 직접 모드 이름으로 바꿔야 합니다.",
                            $"잘못된{combinedRequiredMods}경고".GetHashCode());
                    }
                }
                sheet.Cell(2 + i, 5).Value = entry.Original;
                if (entry.Translated != null)
                {
                    sheet.Cell(2 + i, 6).Value = entry.Translated;
                }

                if (entry.TryGetExtension(Prefabs.ExtensionKeyExtraComment, out object? extension) &&
                    extension is string extensionStr)
                {
                    var comment = sheet.Cell(2 + i, 6).CreateComment();
                    comment.AddText(extensionStr);
                }
            }

            sheet.Style.Font.FontName = "맑은 고딕";
            xlsx.SaveSafely(outPath + ".xlsx");
        }

        public static List<TranslationEntry> FromExcel(string inputPath)
        {
            var xlsx = new XLWorkbook(inputPath);
            var sheet = xlsx.Worksheets.Worksheet(1);
            var translations = new List<TranslationEntry>();
            var rows = sheet.RowsUsed().ToList();
            var headers = rows.First().Cells();

            var colClass = headers.FirstOrDefault(x => x.StrVal() == HeaderClass)
                               ?.WorksheetColumn().ColumnNumber() ??
                           throw new XlsxReadingException(HeaderClass);
            var colNode = headers.FirstOrDefault(x => x.StrVal() == HeaderNode)
                ?.WorksheetColumn().ColumnNumber() ??
                          throw new XlsxReadingException(HeaderNode);
            var colRequiredMods = headers
                .FirstOrDefault(x => x.StrVal() == HeaderRequiredMods)
                ?.WorksheetColumn().ColumnNumber() ?? -1;
            var colOriginal = headers.FirstOrDefault(x => x.StrVal() == HeaderOriginal)
                                  ?.WorksheetColumn().ColumnNumber() ??
                              headers.FirstOrDefault(x => x.StrVal() == "EN [Source string]")
                                  ?.WorksheetColumn().ColumnNumber() ??
                              throw new XlsxReadingException(HeaderOriginal);
            var colTranslated = headers.FirstOrDefault(x => x.StrVal() == HeaderTranslated)
                                    ?.WorksheetColumn().ColumnNumber() ??
                                headers.FirstOrDefault(x => x.StrVal() == "KO [Translation]")
                                    ?.WorksheetColumn().ColumnNumber() ??
                                throw new XlsxReadingException(HeaderTranslated);



            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var className = row.Cell(colClass).StrVal();
                var node = row.Cell(colNode).StrVal();
                RequiredMods? requiredMods = null;
                if (colRequiredMods != -1 && row.Cell(colRequiredMods).Value is { IsText: true } cellRequiredMods)
                {
                    var textRequiredMods = cellRequiredMods.GetText();
                    // 하위 호환성
                    if (textRequiredMods != null && textRequiredMods.Contains('\n'))
                    {
                        requiredMods = new RequiredMods();
                        foreach (var s in textRequiredMods.Split('\n'))
                        {
                            requiredMods.AddAllowedByModName(s);
                        }
                    }
                    else if (textRequiredMods != null)
                    {
                        requiredMods = RequiredMods.FromStringByModNames(textRequiredMods);
                    }
                }
                var original = row.Cell(colOriginal).Value.IsBlank ? "" : row.Cell(colOriginal).StrVal();
                var cellTranslated = row.Cell(colTranslated).Value;
                var translated = cellTranslated.IsText ? (cellTranslated.GetText() == "" ? null : cellTranslated.GetText()) : null;

                var translation = new TranslationEntry(className, node, original,
                    translated, requiredMods);
                translations.Add(translation);
            }
            return translations;
        }

        public static void ToLanguageXml(List<TranslationEntry> translations, bool skipNoTranslation, bool commentOriginal, string fileName, string rootDirPath)
        {
            var languagesDir = PathCombineCreateDir(rootDirPath, "Languages");
            var translationDir = PathCombineCreateDir(languagesDir, Prefabs.TranslationLanguage);
            var defInjected = new List<TranslationEntry>();
            var defInjectedFullListTranslations = new List<TranslationEntry>();
            var keyed = new List<TranslationEntry>();
            var strings = new List<TranslationEntry>();
            var patches = new List<TranslationEntry>();
            var patchedNodeSet = new HashSet<string>();


            foreach (var translation in translations)
            {
                var className = translation.ClassName;

                if (skipNoTranslation && className != "Strings" && string.IsNullOrEmpty(translation.Translated))
                {
                    continue;
                }

                switch (className)
                {
                    case "Keyed":
                        keyed.Add(translation);
                        break;
                    case "Strings":
                        strings.Add(translation);
                        break;
                    default:
                        {
                            if (className.StartsWith("Patches."))
                                patches.Add(translation);
                            else if (Prefabs.FullListTranslationTags.Any(x => translation.Node.Contains(x)))
                                defInjectedFullListTranslations.Add(translation);
                            else
                                defInjected.Add(translation);
                            break;
                        }
                }
            }

            if (skipNoTranslation && patches.Count == 0 && defInjected.Count == 0 &&
                keyed.Count == 0 && translations.Count > 0 && defInjectedFullListTranslations.Count > 0)
            {
                Log.Wrn("번역 데이터가 존재하지 않아 아무것도 추출되지 않습니다.");
            }

            if (patches.Count > 0)
            {
                var outputPath = PathCombineCreateDir(rootDirPath, "Patches");

                var docPatch = new XmlDocument();
                docPatch.AppendElement("Patch");
                var root = docPatch.DocumentElement ?? throw new InvalidOperationException();

                var entryDict = new Dictionary<string, XmlElement>();

                // RequiredMods에 따라 뼈대 사전 생성
                foreach (var translation in CompatManager.DoPostProcessing(patches))
                {
                    var requiredMods = translation.RequiredMods;
                    if (requiredMods == null || entryDict.ContainsKey(requiredMods.ToString()))
                        continue;

                    var aboveNode = root.AppendElement("Operation");
                    foreach (var allowedMod in requiredMods.AllowedMods)
                    {
                        aboveNode.Append(operationFindMod =>
                        {
                            operationFindMod.AppendAttribute("Class", "PatchOperationFindMod");
                            operationFindMod.AppendElement("mods", mods =>
                            {
                                var allowedModSplited = allowedMod.Split(RequiredMods.OR_IDENTIFIER);
                                foreach (var allowedModToken in allowedModSplited)
                                {
                                    if (allowedModToken.Contains("##packageId##"))
                                    {
                                        Log.ErrOnce(
                                            $"Required Mods 열에 잘못된 값이 존재합니다. Patches의 올바른 생성을 위해 엑셀 파일에 있는 해당 문구: \"{allowedModToken}\" 를 직접 모드 이름으로 바꿔야 합니다.",
                                            $"잘못된{allowedModToken}에러".GetHashCode());
                                    }

                                    mods.AppendElement("li", allowedModToken);
                                }
                            });
                            aboveNode = operationFindMod.AppendElement("match");
                        });
                    }

                    foreach (var disallowedMod in requiredMods.DisallowedMods)
                    {
                        aboveNode.Append(operationFindMod =>
                        {
                            operationFindMod.AppendAttribute("Class", "PatchOperationFindMod");
                            operationFindMod.AppendElement("mods", mods =>
                            {
                                var disallowedModSplited = disallowedMod.Split(RequiredMods.OR_IDENTIFIER);
                                foreach (var disallowedModToken in disallowedModSplited)
                                {
                                    if (disallowedModToken.Contains("##packageId##"))
                                    {
                                        Log.ErrOnce(
                                            $"Required Mods 열에 잘못된 값이 존재합니다. Patches의 올바른 생성을 위해 엑셀 파일에 있는 해당 문구: \"{disallowedModToken}\" 를 직접 모드 이름으로 바꿔야 합니다.",
                                            $"잘못된{disallowedModToken}에러".GetHashCode());
                                    }

                                    mods.AppendElement("li", disallowedModToken);
                                }
                            });
                            aboveNode = operationFindMod.AppendElement("nomatch");
                        });
                    }

                    aboveNode.Append(operationSequence =>
                    {
                        operationSequence.AppendAttribute("Class", "PatchOperationSequence");
                        operationSequence.AppendElement("success", "Always");
                        entryDict[requiredMods.ToString()] = operationSequence.AppendElement("operations");
                    });
                }

                foreach (var translation in patches)
                {
                    var requiredMods = translation.RequiredMods;
                    XmlElement operation;
                    if (requiredMods != null)
                    {
                        operation = entryDict[requiredMods.ToString()];
                    }
                    else
                    {
                        operation = root.AppendElement("Operation");
                    }

                    operation.Append(li =>
                    {
                        li.AppendAttribute("Class", "PatchOperationReplace");
                        li.AppendElement("success", "Always");
                        if (commentOriginal)
                            li.AppendComment(
                                $"Original={SecurityElement.Escape(translation.Original).Replace('-', 'ー')}");
                        li.AppendElement("xpath", Utils.GetXpath(translation.ClassName[(translation.ClassName.IndexOf('.') + 1)..], translation.Node));
                        li.AppendElement("value", value =>
                        {
                            var lastNode = translation.Node.Split('.').Last();
                            if (int.TryParse(lastNode, out _)) lastNode = "li";
                            value.AppendElement(lastNode, translation.Translated ?? translation.Original);
                        });
                    });

                }

                docPatch.SaveSafely(Path.Combine(outputPath, fileName + ".xml"));
            }

            if (defInjected.Count > 0)
            {
                var defInjectedDir = PathCombineCreateDir(translationDir, "DefInjected");
                var xmls = new Dictionary<string, XmlDocument>();
                foreach (var translation in CompatManager.DoPostProcessing(defInjected))
                {
                    if (patchedNodeSet.Contains(translation.Node))
                        continue;
                    PathCombineCreateDir(defInjectedDir, translation.ClassName);
                    if (!xmls.TryGetValue(translation.ClassName, out var doc))
                    {
                        doc = new XmlDocument();
                        xmls[translation.ClassName] = doc;
                        doc.AppendElement("LanguageData");
                    }

                    doc.DocumentElement!.Append(languageData =>
                    {
                        if (commentOriginal)
                            languageData.AppendComment($"Original={SecurityElement.Escape(translation.Original).Replace('-', 'ー')}");
                        languageData.AppendElement(translation.Node, t =>
                        {
                            t.InnerText = translation.Translated ?? translation.Original;
                            if (!t.InnerText.Contains("{*")) return;
                            t.InnerText = Regex.Replace(t.InnerText, "\\{\\*(.*?)\\}", match =>
                            {
                                var targetIdentifier = match.Groups[1].Value;
                                var replacement = translations.FirstOrDefault(x => $"{x.ClassName}+{x.Node}" == targetIdentifier);
                                if (replacement != null)
                                    return replacement.Translated ?? replacement.Original;
                                Log.Err($"Pointer: {targetIdentifier}에 대한 원본 Identifier를 찾을 수 없습니다.");
                                return "ERR";
                            });
                        });
                    });

                }

                foreach (var (className, doc) in xmls)
                {
                    var outputPath = Path.Combine(defInjectedDir, className, fileName + ".xml");

                    doc.DoFullListTranslation();
                    doc.SaveSafely(outputPath);
                }
            }

            if (defInjectedFullListTranslations.Count > 0)
            {
                var defInjectedDir = PathCombineCreateDir(translationDir, "DefInjected");
                var xmls = new Dictionary<(string, string), XmlDocument>();
                foreach (var translation in CompatManager.DoPostProcessing(defInjectedFullListTranslations))
                {
                    if (patchedNodeSet.Contains(translation.Node))
                        continue;
                    PathCombineCreateDir(defInjectedDir, translation.ClassName);
                    var nodeParent = translation.Node[..translation.Node.LastIndexOf('.')];
                    if (!xmls.TryGetValue((translation.ClassName, nodeParent), out var doc))
                    {
                        doc = new XmlDocument();
                        xmls[(translation.ClassName, nodeParent)] = doc;
                        doc.AppendElement("LanguageData");
                    }

                    doc.DocumentElement!.Append(languageData =>
                    {
                        if (commentOriginal)
                            languageData.AppendComment($"Original={SecurityElement.Escape(translation.Original).Replace('-', 'ー')}");
                        languageData.AppendElement(translation.Node, t =>
                        {
                            t.InnerText = translation.Translated ?? translation.Original;
                            if (!t.InnerText.Contains("{*")) return;
                            t.InnerText = Regex.Replace(t.InnerText, "\\{\\*(.*?)\\}", match =>
                            {
                                var targetIdentifier = match.Groups[1].Value;
                                var replacement = translations.FirstOrDefault(x => $"{x.ClassName}+{x.Node}" == targetIdentifier);
                                if (replacement != null)
                                    return replacement.Translated ?? replacement.Original;
                                Log.Err($"Pointer: {targetIdentifier}에 대한 원본 Identifier를 찾을 수 없습니다.");
                                return "ERR";
                            });
                        });
                    });

                }

                foreach (var ((className, nodeParent), doc) in xmls)
                {
                    var tokens = nodeParent.Split('.');
                    var outputPath = Path.Combine(defInjectedDir, className,
                        fileName + $"-{tokens[0]}{tokens.Last()}" + ".xml");

                    doc.DoFullListTranslation();
                    doc.SaveSafely(outputPath);
                }
            }

            if (keyed.Count > 0)
            {
                var keyedDir = PathCombineCreateDir(translationDir, "Keyed");
                var xmls = new Dictionary<string, XmlDocument>();
                foreach (var translation in keyed)
                {
                    var idxSep = translation.Node.IndexOf('|');
                    var key = idxSep != -1 ? translation.Node.Split('|')[0] : "default";
                    var nodeName = idxSep != -1 ? translation.Node[(idxSep + 1)..] : translation.Node;

                    if (!xmls.TryGetValue(key, out var doc))
                    {
                        doc = new XmlDocument();
                        xmls[key] = doc;
                        doc.AppendElement("LanguageData");
                    }

                    doc.DocumentElement!.Append(languageData =>
                    {
                        if (commentOriginal)
                            languageData.AppendComment($"{Prefabs.OriginalLanguage}={SecurityElement.Escape(translation.Original).Replace('-', 'ー')}");
                        languageData.AppendElement(nodeName, translation.Translated ?? translation.Original);
                    });
                }

                foreach (var (_, doc) in xmls)
                {
                    var outputPath = Path.Combine(keyedDir, $"{fileName}.xml");
                    doc.SaveSafely(outputPath);
                }
            }

            if (strings.Count > 0)
            {
                var stringDir = PathCombineCreateDir(translationDir, "Strings");
                var txts = new Dictionary<string, List<string>>();
                foreach (var translation in strings)
                {
                    var className = translation.Node[..translation.Node.LastIndexOf('.')];
                    if (!txts.TryGetValue(className, out var lines))
                    {
                        lines = new List<string>();
                        txts[className] = lines;
                    }

                    lines.Add(translation.Translated ?? translation.Original);
                }

                foreach (var (className, lines) in txts)
                {
                    var key = className[..className.LastIndexOf('.')].Replace('.', '\\');
                    var outputPath = PathCombineCreateDir(stringDir, key);
                    var fileNameTxt = Path.Combine(outputPath, $"{className.Split('.').Last()}") + ".txt";
                    lines.SaveSafely(fileNameTxt);
                }
            }
        }

        public static List<TranslationEntry> FromLanguageXml(string rootPath)
        {
            var translationsDir = Path.Combine(rootPath, "Languages", Prefabs.TranslationLanguage);
            var defInjectedDir = Path.Combine(translationsDir, "DefInjected");
            var keyedDir = Path.Combine(translationsDir, "Keyed");
            var stringsDir = Path.Combine(translationsDir, "Strings");
            // var patchesDir = Path.Combine(rootPath, "Patches");

            var translations = new List<TranslationEntry>();

            foreach (var filePath in DescendantFiles(defInjectedDir).Where(x => x.ToLower().EndsWith(".xml")))
            {
                var className = Path.GetRelativePath(defInjectedDir, filePath).Split(Path.DirectorySeparatorChar).First();
                try
                {
                    var doc = ReadXml(filePath);
                    foreach (XmlElement node in doc.DocumentElement!.ChildNodes)
                    {
                        var name = node.Name;
                        translations.Add(new TranslationEntry(className, name, string.Empty, node.InnerText, null));
                    }
                }
                catch (Exception e)
                {
                    Log.Err($"{filePath}를 읽는 중 에러 발생: {e.Message}");
                    throw;
                }
            }

            var keyed = new ExtractableFolder(ModMetadata.Emptry, keyedDir, null);
            translations.AddRange(Extractor.ExtractKeyed(keyed).Select(x => x with{Translated = x.Original, Original = ""}));

            var strings = new ExtractableFolder(ModMetadata.Emptry, stringsDir, null);
            translations.AddRange(Extractor.ExtractStrings(strings).Select(x => x with{Translated = x.Original, Original = ""}));

            return translations;
        }

        private static void SaveSafely(this XLWorkbook xlsx, string path)
        {
            if (!File.Exists(path))
            {
                xlsx.SaveAs(path);
                return;
            }

            switch (Prefabs.Policy)
            {
                case Prefabs.DuplicatesPolicy.Stop:
                    var stopCallback = Prefabs.StopCallbackXlsx;
                    if (stopCallback != null)
                        stopCallback(xlsx, path);
                    else
                        throw new ArgumentNullException(nameof(stopCallback));
                    return;
                case Prefabs.DuplicatesPolicy.Overwrite:
                    try
                    {
                        xlsx.SaveAs(path);
                    }
                    catch (IOException)
                    {
                        Log.Err($"{Path.GetFileName(path)}: 파일이 이미 사용 중이기 때문에 파일을 저장할 수 없었습니다. 종료 후 재시도 해주세요.");
                    }
                    return;
                case Prefabs.DuplicatesPolicy.KeepOriginal:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SaveSafely(this XmlDocument doc, string path)
        {
            doc.InsertBefore(doc.CreateXmlDeclaration("1.0", "utf-8", null), doc.DocumentElement);
            if (!File.Exists(path))
            {
                doc.Save(path);
                return;
            }

            switch (Prefabs.Policy)
            {
                case Prefabs.DuplicatesPolicy.Stop:
                    var stopCallback = Prefabs.StopCallbackXml;
                    if (stopCallback != null)
                        stopCallback(doc, path);
                    else
                        throw new ArgumentNullException(nameof(stopCallback));
                    return;
                case Prefabs.DuplicatesPolicy.Overwrite:
                    doc.Save(path);
                    return;
                case Prefabs.DuplicatesPolicy.KeepOriginal:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SaveSafely(this IEnumerable<string> lines, string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllLines(path, lines);
                return;
            }

            switch (Prefabs.Policy)
            {
                case Prefabs.DuplicatesPolicy.Stop:
                    var stopCallback = Prefabs.StopCallbackTxt;
                    if (stopCallback != null)
                        stopCallback(lines, path);
                    else
                        throw new ArgumentNullException(nameof(stopCallback));
                    return;
                case Prefabs.DuplicatesPolicy.Overwrite:
                    File.WriteAllLines(path, lines);
                    return;
                case Prefabs.DuplicatesPolicy.KeepOriginal:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private static string PathCombineCreateDir(params string[] paths)
        {
            var dir = Path.Combine(paths);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private static void DoFullListTranslation(this XmlDocument defInjectedDoc)
        {
            var patterns = Prefabs.FullListTranslationTags.Select(x => $".+?\\.{x}\\.\\d+").ToList();

            var fullListdic = new Dictionary<string, XmlNode>();
            var removedNodesDic = new Dictionary<string, List<XmlNode>>();
            foreach (XmlNode childNode in defInjectedDoc.DocumentElement!.ChildNodes)
            {
                var nodeName = childNode.Name;
                if (!patterns.Any(x => Regex.IsMatch(nodeName, x)))
                    continue;
                nodeName = nodeName[..nodeName.LastIndexOf('.')];
                if (!fullListdic.TryGetValue(nodeName, out var fullList))
                {
                    fullList = defInjectedDoc.CreateElement(nodeName);
                    fullListdic[nodeName] = fullList;
                }

                if (!removedNodesDic.TryGetValue(nodeName, out var removedList))
                {
                    removedList = new List<XmlNode>();
                    removedNodesDic[nodeName] = removedList;
                }

                var li = fullList.AppendElement("li");
                li.InnerText = childNode.InnerText;
                removedList.Add(childNode);
            }


            foreach (var (key, fullListNode) in fullListdic)
            {
                var removedList = removedNodesDic[key];

                defInjectedDoc.DocumentElement!.InsertAfter(fullListNode, removedList.Last());
                foreach (var xmlNode in removedList)
                {
                    defInjectedDoc.DocumentElement!.RemoveChild(xmlNode);
                }
            }
        }

        internal static XmlDocument ReadXml(string filePath)
        {
            var contents = File.ReadAllText(filePath);
            var readerSettings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                CheckCharacters = false
            };
            using var stringReader = new StringReader(contents);
            using var xmlReader = XmlReader.Create(stringReader, readerSettings);
            var childDoc = new XmlDocument();
            childDoc.Load(xmlReader);
            return childDoc;
        }

        internal static IEnumerable<string> DescendantFiles(string root)
        {
            if (!Directory.Exists(root))
                yield break;

            var q = new Queue<string>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var curPath = q.Dequeue();
                foreach (var subDir in Directory.GetDirectories(curPath).OrderBy(x => x))
                {
                    q.Enqueue(subDir);
                }

                foreach (var file in Directory.EnumerateFiles(curPath).OrderBy(x => x))
                {
                    yield return file;
                }
            }
        }
    }
}
