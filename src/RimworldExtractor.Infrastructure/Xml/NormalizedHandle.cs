using System.Text;
using System.Text.RegularExpressions;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Pure string normalization for translation-handle values. Port of
/// <c>Extractor.DefsUtils.cs:233-281</c> — used when a <c>TranslationHandle</c> produces a
/// path segment from an XML element's inner text.
/// </summary>
public static partial class NormalizedHandle
{
    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex BracePattern();

    private static readonly char[] ValidChars =
        "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890-_".ToCharArray();

    /// <summary>
    /// Normalizes <paramref name="handle"/> into a valid XML path segment:
    /// replaces whitespace with underscores, strips braces and invalid chars,
    /// collapses consecutive underscores, trims edge underscores, and prefixes
    /// <c>_</c> when the result is all-digits.
    /// </summary>
    public static string Normalize(string handle)
    {
        if (string.IsNullOrEmpty(handle))
            return handle;

        handle = handle.Trim();
        handle = handle.Replace(' ', '_');
        handle = handle.Replace('\n', '_');
        handle = handle.Replace("\r", string.Empty);
        handle = handle.Replace('\t', '_');
        handle = handle.Replace(".", string.Empty);

        if (handle.Contains('-'))
            handle = handle.Replace("-", string.Empty);

        if (handle.Contains('{'))
            handle = BracePattern().Replace(handle, string.Empty);

        // Strip characters not in the allowed set
        var sb = new StringBuilder(handle.Length);
        foreach (char c in handle)
        {
            if (Array.IndexOf(ValidChars, c) >= 0)
                sb.Append(c);
        }

        handle = sb.ToString();

        // Collapse consecutive underscores
        sb.Clear();
        for (int j = 0; j < handle.Length; j++)
        {
            if (j == 0 || handle[j] != '_' || handle[j - 1] != '_')
                sb.Append(handle[j]);
        }

        handle = sb.ToString().Trim('_');

        if (!string.IsNullOrEmpty(handle) && handle.All(char.IsDigit))
            handle = "_" + handle;

        return handle;
    }
}
