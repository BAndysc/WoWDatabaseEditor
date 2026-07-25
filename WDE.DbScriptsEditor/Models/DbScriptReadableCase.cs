using System;

namespace WDE.DbScriptsEditor.Models
{
    // Applies sentence casing to a rendered readable, aware of the FormattedTextBlock markup so it
    // works on both the plain and the clickable ([s=N]…[/s], [p=N]…[/p]) forms: the first word (the
    // actor) and, for "{who}: {what}" descriptions, the first word of the action after the colon get
    // an uppercase initial. A parenthetical target ("… (nearest creature 5)") is left lowercase.
    public static class DbScriptReadableCase
    {
        public static bool StartsWithActorToken(string? description) =>
            description != null &&
            (description.StartsWith("{source}:", StringComparison.Ordinal) ||
             description.StartsWith("{target}:", StringComparison.Ordinal) ||
             description.StartsWith("{player}:", StringComparison.Ordinal));

        public static string SentenceCase(string s, bool hasActorColon)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            var chars = s.ToCharArray();
            CapitalizeAt(chars, 0);
            if (hasActorColon)
            {
                var sep = s.IndexOf(": ", StringComparison.Ordinal);
                if (sep >= 0)
                    CapitalizeAt(chars, sep + 2);
            }
            return new string(chars);
        }

        // Uppercases the initial of the first real (non-space, non-markup) character at/after `from`.
        // Skips whitespace and [markup] tags; if that first character is not a letter (e.g. a number),
        // nothing is changed.
        private static void CapitalizeAt(char[] chars, int from)
        {
            var i = from;
            while (i < chars.Length)
            {
                if (chars[i] == '[')
                {
                    while (i < chars.Length && chars[i] != ']')
                        i++;
                    if (i < chars.Length)
                        i++; // step past ']'
                    continue;
                }
                if (char.IsWhiteSpace(chars[i]))
                {
                    i++;
                    continue;
                }
                if (char.IsLetter(chars[i]))
                    chars[i] = char.ToUpperInvariant(chars[i]);
                return; // reached the first real character
            }
        }
    }
}
