using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Legacy;

/// <summary>
/// Parses / formats the legacy Prefabs.dat DSL for extraction rules:
/// <c>tag+white,list-black,list</c>. First <c>+</c> or <c>-</c> marks the tag boundary;
/// alternating segments populate whitelist / blacklist.
/// </summary>
public static class LegacyExtractionRuleParser
{
    public static ExtractionRule Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Raw rule must be non-empty.", nameof(raw));

        var plusIndex = raw.IndexOf('+');
        var minusIndex = raw.IndexOf('-');

        if (plusIndex == -1 && minusIndex == -1)
        {
            return new ExtractionRule(raw);
        }

        var firstSep = (plusIndex != -1 && minusIndex != -1)
            ? Math.Min(plusIndex, minusIndex)
            : Math.Max(plusIndex, minusIndex);

        var tag = raw[..firstSep];
        var remain = raw[firstSep..];

        var whitelist = new HashSet<string>();
        var blacklist = new HashSet<string>();

        int i = 0;
        while (i < remain.Length)
        {
            char mode = remain[i];
            int nextPlus = remain.IndexOf('+', i + 1);
            int nextMinus = remain.IndexOf('-', i + 1);
            int nextSep = (nextPlus == -1 && nextMinus == -1) ? remain.Length
                : (nextPlus == -1) ? nextMinus
                : (nextMinus == -1) ? nextPlus
                : Math.Min(nextPlus, nextMinus);

            var content = remain[(i + 1)..nextSep];
            var items = content.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var target = (mode == '+') ? whitelist : blacklist;
            foreach (var item in items) target.Add(item.Trim());

            i = nextSep;
        }

        return new ExtractionRule(tag, whitelist, blacklist);
    }

    public static string Format(ExtractionRule rule)
    {
        var sb = new System.Text.StringBuilder(rule.Tag);
        if (rule.Whitelist.Count > 0)
        {
            sb.Append('+');
            sb.Append(string.Join(',', rule.Whitelist.OrderBy(x => x, StringComparer.Ordinal)));
        }
        if (rule.Blacklist.Count > 0)
        {
            sb.Append('-');
            sb.Append(string.Join(',', rule.Blacklist.OrderBy(x => x, StringComparer.Ordinal)));
        }
        return sb.ToString();
    }
}
