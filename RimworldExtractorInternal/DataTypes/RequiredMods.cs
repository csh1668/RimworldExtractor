using DocumentFormat.OpenXml.Vml.Office;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace RimworldExtractorInternal.DataTypes
{
    public class RequiredMods : IAdditionOperators<RequiredMods?, RequiredMods?, RequiredMods?>, ICloneable
    {
        public const string AND_IDENTIFIER = " && ";
        public const string OR_IDENTIFIER = " || ";
        public const string LEFT_BRACKET = " (( ";
        public const string RIGHT_BRACKET = " )) ";
        public const string NOT_IDENTIFIER = " ~~ ";
        public const string PACKAGE_ID_PREFIX = "##packageId##";
        public const string MOD_NAME_PREFIX = "##modName##";
        private readonly HashSet<RequiredModEntry> allowedMods = new();
        private readonly HashSet<RequiredModEntry> disallowedMods = new();

        public int CountAllowed => allowedMods.Count;
        public int CountDisallowed => disallowedMods.Count;
        public IEnumerable<string> AllowedMods => allowedMods.Select(allowedMod => (string)allowedMod);
        public IEnumerable<string> DisallowedMods => disallowedMods.Select(disallowedMod => (string)disallowedMod);

        public RequiredMods() { }

        public RequiredMods(RequiredMods? other) : this()
        {
            if (other != null)
            {
                foreach (var allowedMod in other.allowedMods)
                {
                    this.allowedMods.Add((RequiredModEntry)allowedMod.Clone());
                }

                foreach (var disallowedMod in other.disallowedMods)
                {
                    this.disallowedMods.Add((RequiredModEntry)disallowedMod.Clone());
                }
            }
        }

        public void AddAllowedByModName(string modName)
        {
            allowedMods.Add(RequiredModEntry.FromModName(modName));
        }

        public void AddAllowedByModNames(IEnumerable<string> modNames)
        {
            allowedMods.Add(RequiredModEntry.FromModNames(modNames));
        }

        public void AddAllowedByPackageId(string packageId)
        {
            allowedMods.Add(RequiredModEntry.FromPackageId(packageId));
        }

        public void AddAllowedByPackageIds(IEnumerable<string> packageIds)
        {
            allowedMods.Add(RequiredModEntry.FromPackageIds(packageIds));
        }

        public void AddDisallowedByModName(string modName)
        {
            disallowedMods.Add(RequiredModEntry.FromModName(modName));
        }

        public void AddDisallowedByModNames(IEnumerable<string> modNames)
        {
            disallowedMods.Add(RequiredModEntry.FromModNames(modNames));
        }

        public void AddDisallowedByPackageId(string packageId)
        {
            disallowedMods.Add(RequiredModEntry.FromPackageId(packageId));
        }

        public void AddDisallowedByPackageIds(IEnumerable<string> packageIds)
        {
            disallowedMods.Add(RequiredModEntry.FromPackageIds(packageIds));
        }

        public bool ContainsAllowed(string modName)
        {
            return allowedMods.Any(entry => entry.MatchesByModName(modName));
        }
        public bool ContainsDisallowed(string modName)
        {
            return disallowedMods.Any(entry => entry.MatchesByModName(modName));
        }

        public bool HasSameElements(RequiredMods other)
        {
            return this.AllowedMods.HasSameElements(other.AllowedMods) &&
                   this.DisallowedMods.HasSameElements(other.DisallowedMods);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (allowedMods.Count > 0)
            {
                sb.Append(string.Join(AND_IDENTIFIER, allowedMods.Select(x =>
                {
                    var str = x.ToString(out var hasTwoOrMore);
                    if (hasTwoOrMore)
                    {
                        str = LEFT_BRACKET + str + RIGHT_BRACKET;
                    }

                    return str;
                })));
            }

            if (disallowedMods.Count > 0)
            {
                if (allowedMods.Count > 0)
                {
                    sb.Append(AND_IDENTIFIER);
                }
                sb.Append(NOT_IDENTIFIER);
                if (disallowedMods.Count > 1)
                {
                    sb.Append(LEFT_BRACKET);
                }

                sb.Append(string.Join(AND_IDENTIFIER, disallowedMods.Select(x =>
                {
                    var str = x.ToString(out var hasTwoOrMore);
                    if (hasTwoOrMore)
                    {
                        str = LEFT_BRACKET + str + RIGHT_BRACKET;
                    }

                    return str;
                })));
                if (disallowedMods.Count > 1)
                {
                    sb.Append(RIGHT_BRACKET);
                }
            }

            return sb.ToString();
        }

        public static RequiredMods FromStringByModNames(string modNames)
        {
            return FromStringInternal(modNames, 
                modName => RequiredModEntry.FromModNames(modName.Split(OR_IDENTIFIER)));
        }

        public static RequiredMods FromStringByPackageIds(string packageIds)
        {
            return FromStringInternal(packageIds,
                packageId => RequiredModEntry.FromPackageIds(packageIds.Split(OR_IDENTIFIER)));
        }
        private static RequiredMods FromStringInternal(string str, Func<string, RequiredModEntry> converter)
        {
            var result = new RequiredMods();

            void ParseDisallowed(string input)
            {
                input = input[NOT_IDENTIFIER.Length..];
                input = RemoveBrackets(input);
                var tokens = input.Split(AND_IDENTIFIER);
                foreach (var token in tokens)
                {

                    result.disallowedMods.Add(converter(RemoveBrackets(token)));
                }
            }

            void ParseAllowed(string input)
            {
                var tokens = input.Split(AND_IDENTIFIER);
                foreach (var token in tokens)
                {
                    result.allowedMods.Add(converter(RemoveBrackets(token)));
                }
            }
            
            // NOT만 있는 경우
            if (!str.Contains(AND_IDENTIFIER + NOT_IDENTIFIER) && str.Contains(NOT_IDENTIFIER))
            {
                ParseDisallowed(str);
            }
            // NOT만 없는 경우
            else if (!str.Contains(NOT_IDENTIFIER))
            {
                ParseAllowed(str);
            }
            // 둘 다 있는 경우
            else
            {
                var splited = str.Split(AND_IDENTIFIER + NOT_IDENTIFIER);
                var allow = splited[0];
                var disallow = splited[1];
                ParseAllowed(allow);
                ParseDisallowed(disallow);
            }
            return result;
        }

        private static string RemoveBrackets(string str)
        {
            if (str.StartsWith(LEFT_BRACKET))
            {
                str = str[LEFT_BRACKET.Length..];
            }

            if (str.EndsWith(RIGHT_BRACKET))
            {
                str = str[..^RIGHT_BRACKET.Length];
            }
            return str;
        }


        public RequiredMods Concat(RequiredMods? other)
        {
            if (other == null)
                return this;
            var result = new RequiredMods(this);
            foreach (var otherAllowedMod in other.allowedMods)
            {
                result.allowedMods.Add(otherAllowedMod);
            }

            foreach (var otherDisallowedMod in other.disallowedMods)
            {
                result.disallowedMods.Add(otherDisallowedMod);
            }

            return result;
        }
        public object Clone()
        {
            var result = new RequiredMods(this);
            return result;
        }

        private class RequiredModEntry : ICloneable
        {
            private readonly HashSet<PackageIdModNamePair> _items = new();
            public RequiredModEntry() { }

            public static RequiredModEntry FromPackageId(string packageId) => FromPackageIds(Enumerable.Repeat(packageId, 1));

            public static RequiredModEntry FromPackageIds(IEnumerable<string> packageIds)
            {
                var result = new RequiredModEntry();
                foreach (var packageId in packageIds)
                {
                    result._items.Add(PackageIdModNamePair.FromPackageId(packageId));
                }

                return result;
            }

            public static RequiredModEntry FromModName(string modNames) => FromModNames(Enumerable.Repeat(modNames, 1));

            public static RequiredModEntry FromModNames(IEnumerable<string> modsNames)
            {
                var result = new RequiredModEntry();
                foreach (var modName in modsNames)
                {
                    result._items.Add(PackageIdModNamePair.FromModName(modName));
                }

                return result;
            }

            private RequiredModEntry(RequiredModEntry other)
            {
                foreach (var pair in other._items)
                {
                    _items.Add(pair);
                }
            }

            public void Reset()
            {
                _items.Clear();
            }

            public void AddByPackageId(string packageId)
            {
                _items.Add(PackageIdModNamePair.FromPackageId(packageId));
            }

            public void AddRangeByPackageIds(IEnumerable<string> packageIds)
            {
                foreach (var item in packageIds)
                {
                    _items.Add(PackageIdModNamePair.FromPackageId(item));
                }
            }

            public bool MatchesByPackageId(string packageId)
            {
                return _items.Any(x => x.PackageId == packageId);
            }

            public bool MatchesByModName(string modName)
            {
                return _items.Any(x => x.ModName == modName);
            }

            public object Clone()
            {
                var result = new RequiredModEntry(this);
                return result;
            }

            public override bool Equals(object? obj)
            {
                return base.Equals(obj);
            }

            protected bool Equals(RequiredModEntry other)
            {
                return _items.HasSameElements(other._items);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                foreach (var item in _items)
                {
                    hash.Add(item.GetHashCode());
                }
                return hash.ToHashCode();
            }

            public static implicit operator RequiredModEntry(string modNames)
            {
                if (modNames.StartsWith(LEFT_BRACKET) && modNames.EndsWith(RIGHT_BRACKET))
                {
                    modNames = modNames.Replace(LEFT_BRACKET, "").Replace(RIGHT_BRACKET, "");
                }

                return FromModNames(modNames.Split(OR_IDENTIFIER));
            }

            public static implicit operator string(RequiredModEntry entry)
            {
                return entry.ToString();
            }

            public override string ToString()
            {
                return $"{string.Join(OR_IDENTIFIER, _items.Select(x => x.ModName))}";
            }

            public string ToString(out bool hasTwoOrMore)
            {
                hasTwoOrMore = _items.Count > 1;
                return ToString();
            }
        }

        private struct PackageIdModNamePair
        {
            public string PackageId
            {
                get
                {
                    if (_packageId != null)
                        return _packageId;
                    if (_modName != null)
                    {
                        if (_modName.StartsWith(PACKAGE_ID_PREFIX))
                        {
                            _packageId = _modName.Substring(PACKAGE_ID_PREFIX.Length, _modName.Length - PACKAGE_ID_PREFIX.Length);
                            return _packageId;
                        }
                        else
                        {
                            _packageId = ModLister.GetModMetadataByModName(_modName)?.PackageId;
                            return _packageId ?? "##modName##" + _modName;
                        }
                    }

                    throw new InvalidOperationException("Both PackageId and ModName are null.");
                }
            }

            public string ModName
            {
                get
                {
                    if (_modName != null)
                        return _modName;
                    if (_packageId != null)
                    {
                        if (_packageId.StartsWith(MOD_NAME_PREFIX))
                        {
                            _modName = _packageId.Substring(MOD_NAME_PREFIX.Length,
                                _packageId.Length + MOD_NAME_PREFIX.Length);
                            return _modName;
                        }
                        if (ModLister.TryGetModMetadataByPackageId(_packageId, out var modMetadata) &&
                            modMetadata != null)
                        {
                            _modName = modMetadata.ModName;
                        }
                        else
                        {
                            _modName = "##packageId##" + _packageId;
                        }
                        return _modName;
                    }

                    throw new InvalidOperationException("Both PackageId and ModName are null");
                }
            }

            private string? _packageId = null;
            private string? _modName = null;


            private PackageIdModNamePair(string? packageId, string? modName)
            {
                this._packageId = packageId;
                this._modName = modName;
            }

            public static PackageIdModNamePair FromPackageId(string packageId)
            {
                var result = new PackageIdModNamePair(packageId, null);
                return result;
            }

            public static PackageIdModNamePair FromModName(string modName)
            {
                var result = new PackageIdModNamePair(null, modName);
                return result;
            }

            public static PackageIdModNamePair FromModMetadata(ModMetadata modMetadata)
            {
                var result = new PackageIdModNamePair(modMetadata.PackageId, modMetadata.ModName);
                return result;
            }

            public readonly override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(this._packageId);
                hashCode.Add(this._modName);
                return hashCode.ToHashCode();
            }

            public readonly override bool Equals(object? obj)
            {
                return GetHashCode() == obj?.GetHashCode();
            }
        }

        public static RequiredMods? operator +(RequiredMods? left, RequiredMods? right)
        {
            if (left == null && right == null)
            {
                return null;
            }
            var result = new RequiredMods(left);
            return result.Concat(right);
        }
    }
}
