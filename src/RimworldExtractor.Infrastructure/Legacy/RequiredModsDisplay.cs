using System.Text;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Infrastructure.Legacy;

/// <summary>
/// Produces a deterministic human-readable string representation of a <see cref="RequiredMods"/>
/// instance for use in Excel cells and similar display contexts.
/// </summary>
/// <remarks>
/// Format: <c>allowed[a || b &amp;&amp; c]|disallowed[x]</c> where each AND-group is separated
/// by <c>" &amp;&amp; "</c> and each OR alternative within a group by <c>" || "</c>.
/// When a section is empty it is omitted.
/// </remarks>
public static class RequiredModsDisplay
{
    /// <summary>
    /// Converts <paramref name="requiredMods"/> to a display string.
    /// Returns an empty string for a null or empty value.
    /// </summary>
    public static string ToDisplayString(RequiredMods? requiredMods)
    {
        if (requiredMods is null || requiredMods.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder();

        if (requiredMods.Allowed.Count > 0)
        {
            sb.Append("allowed[");
            AppendGroups(sb, requiredMods.Allowed);
            sb.Append(']');
        }

        if (requiredMods.Disallowed.Count > 0)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append("disallowed[");
            AppendGroups(sb, requiredMods.Disallowed);
            sb.Append(']');
        }

        return sb.ToString();
    }

    private static void AppendGroups(StringBuilder sb, IReadOnlyList<IReadOnlyList<ModReference>> groups)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (i > 0) sb.Append(" && ");
            IReadOnlyList<ModReference> group = groups[i];
            for (int j = 0; j < group.Count; j++)
            {
                if (j > 0) sb.Append(" || ");
                sb.Append(group[j].Value);
            }
        }
    }
}
