using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WDE.DbScriptsEditor.Models
{
    // Expands SmartScript-style conditional tokens in command descriptions:
    //   {Param name:choose(v1,v2,...):option1|option2|...|default}
    // The parameter's RAW column value (as an invariant string) is compared against v1..vN and
    // the matching option is emitted; when none matches, the extra trailing option is used (or
    // nothing, when there is none). Options may contain nested {Param} tokens and nested choose
    // tokens. Plain {Param} tokens are left untouched for the regular replacement pass.
    public static class DbScriptDescriptionChoose
    {
        private static readonly Regex HeadRegex =
            new(@"^([A-Za-z0-9 _]+?):choose\(([^()]*)\):(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

        public static string Expand(string description, Func<string, string?> rawValueByName)
        {
            if (!description.Contains(":choose(", StringComparison.Ordinal))
                return description;

            var sb = new StringBuilder(description.Length);
            var i = 0;
            while (i < description.Length)
            {
                if (description[i] != '{')
                {
                    sb.Append(description[i]);
                    i++;
                    continue;
                }

                var end = FindBalancedEnd(description, i);
                if (end < 0)
                {
                    sb.Append(description, i, description.Length - i);
                    break;
                }

                var token = description.Substring(i + 1, end - i - 1);
                var m = HeadRegex.Match(token);
                // plain {Param} token (or unknown parameter): keep verbatim for the second pass
                if (!m.Success || rawValueByName(m.Groups[1].Value.Trim()) is not { } value)
                {
                    sb.Append(description, i, end - i + 1);
                    i = end + 1;
                    continue;
                }

                var args = m.Groups[2].Value.Split(',');
                var options = SplitTopLevel(m.Groups[3].Value);
                var chosen = "";
                var matched = false;
                for (var a = 0; a < args.Length && a < options.Count; a++)
                {
                    if (string.Equals(args[a].Trim(), value, StringComparison.Ordinal))
                    {
                        chosen = options[a];
                        matched = true;
                        break;
                    }
                }
                if (!matched && options.Count > args.Length)
                    chosen = options[args.Length];

                sb.Append(Expand(chosen, rawValueByName));
                i = end + 1;
            }
            return sb.ToString();
        }

        private static int FindBalancedEnd(string text, int start)
        {
            var depth = 0;
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}' && --depth == 0)
                    return i;
            }
            return -1;
        }

        private static List<string> SplitTopLevel(string text)
        {
            var result = new List<string>();
            var depth = 0;
            var last = 0;
            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '{': depth++; break;
                    case '}': depth--; break;
                    case '|' when depth == 0:
                        result.Add(text.Substring(last, i - last));
                        last = i + 1;
                        break;
                }
            }
            result.Add(text.Substring(last));
            return result;
        }
    }
}
