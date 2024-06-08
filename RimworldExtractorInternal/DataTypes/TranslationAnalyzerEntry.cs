using RimworldExtractorInternal.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.DataTypes
{
    public class TranslationAnalyzerEntry
    {
        public string FilePath;
        public ModMetadata? Metadata;
        public List<TranslationEntry> OriginalTranslations;
        public List<TranslationEntry>? NewTranslations;
        public SaveMethodEnum SaveMethod;

        public bool HasChanges => Metadata != null && Changes.Any();
        public bool Invalid => OriginalTranslations.Count == 0;

        public IEnumerable<ChangeRecord> Changes
        {
            get
            {
                if (_changesCached == null)
                {
                    _changesCached = new List<ChangeRecord>();
                    var usedSet = new HashSet<TranslationEntry>();
                    foreach (var origEntry in OriginalTranslations)
                    {
                        // 새 추출 결과에도 같은 노드가 있다면
                        var pairEntry = NewTranslations?.FirstOrDefault(x =>
                            x.ClassName == origEntry.ClassName && x.Node == origEntry.Node);
                        if (pairEntry != null)
                        {
                            // 원본 원문과 재추출 원문이 다르다면
                            if (origEntry.Original.Length > 0 && origEntry.Original != pairEntry.Original)
                            {
                                _changesCached.Add(new ChangeRecord(origEntry, pairEntry, ChangeReason.ChangedOriginal));
                            }
                            // 원문 원본이 소실되었다면
                            else if (origEntry.Original.Length == 0)
                            {
                                _changesCached.Add(new ChangeRecord(origEntry, pairEntry, ChangeReason.FillOriginal));
                            }

                            usedSet.Add(pairEntry);
                        }
                        // 원문이 없다면
                        else if (NewTranslations != null && !string.IsNullOrEmpty(origEntry.Translated))
                        {
                            _changesCached.Add(new ChangeRecord(origEntry, null, ChangeReason.RemoveNode));
                        }
                    }

                    if (NewTranslations != null)
                    {
                        foreach (var newTranslation in NewTranslations.Where(x => !usedSet.Contains(x)))
                        {
                            _changesCached.Add(new ChangeRecord(null, newTranslation, ChangeReason.AddedNewly));
                        }
                    }
                }

                return _changesCached;
            }
        }

        public string ChangesString
        {
            get
            {
                int cntChangedOriginal = 0, cntFillOriginal = 0, cntRemoveNode = 0, cntAddedNewly = 0;
                var changes = Changes.ToList();
                foreach (var changeRecord in changes)
                {
                    switch (changeRecord.Reason)
                    {
                        case ChangeReason.ChangedOriginal:
                            cntChangedOriginal++;
                            break;
                        case ChangeReason.FillOriginal:
                            cntFillOriginal++;
                            break;
                        case ChangeReason.RemoveNode:
                            cntRemoveNode++;
                            break;
                        case ChangeReason.AddedNewly:
                            cntAddedNewly++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return
                    $"\ud83d\udd04:{cntChangedOriginal} \ud83d\udd27:{cntFillOriginal} \u2796:{cntRemoveNode} \u2795:{cntAddedNewly}";
            }
        }


        public TranslationAnalyzerEntry(string path)
        {
            FilePath = path;
            SaveMethod = SaveMethodEnum.Append;
            Metadata = TranslationAnalyzerTool.GetModMetadataFromFilePath(path);
            try
            {
                OriginalTranslations = IO.FromExcel(path);
            }
            catch (XlsxHeaderReadingException e)
            {
                Log.Err($"올바른 림왈도 서식을 가진 엑셀 파일이 아닙니다: {path}, {e.Message}");
                OriginalTranslations = new List<TranslationEntry>();
            }
            catch (IOException e)
            {
                Log.Err($"엑셀 파일이 열려 있어서 읽을 수 없었습니다. 닫고 다시 시도해주세요: {path}");
                OriginalTranslations = new List<TranslationEntry>();
            }
        }

        public void ResetChanges()
        {
            _changesCached = null;
        }

        public void ReExtract(List<ExtractableFolder> selectedFolders, List<ModMetadata> referenceMods)
        {
            if (Metadata == null)
            {
                throw new NullReferenceException();
            }

            NewTranslations = Extractor.ExtractTranslationData(Metadata, selectedFolders, referenceMods);
        }

        public void MergeTranslation()
        {
            if (NewTranslations == null)
                throw new NullReferenceException();
            var newLst = new List<TranslationEntry>();
            foreach (var entry in NewTranslations)
            {
                var origEntry = OriginalTranslations.FirstOrDefault(x => x.ClassNode == entry.ClassNode);
                if (origEntry != null)
                    newLst.Add(entry with { Translated = origEntry.Translated });
                else
                    newLst.Add(entry);
            }

            NewTranslations = newLst;
        }

        public record ChangeRecord(TranslationEntry? Orig, TranslationEntry? New, ChangeReason Reason);
        public enum ChangeReason : byte
        {
            ChangedOriginal = 0, FillOriginal, RemoveNode, AddedNewly
        }

        public enum SaveMethodEnum : byte
        {
            Append, Overwrite, RewriteNewFile, New
        }

        private List<ChangeRecord>? _changesCached;
    }
}
