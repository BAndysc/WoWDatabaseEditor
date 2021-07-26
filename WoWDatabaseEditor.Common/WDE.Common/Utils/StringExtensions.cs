using System.Text;

namespace WDE.Common.Utils
{
    public static class StringExtensions
    {
        public static string RemoveTags(this string s)
        {
            StringBuilder result = new();
            State state = State.Text;
            foreach (var letter in s)
            {
                if (letter == '\\' && state == State.Text)
                {
                    state = State.Slash;
                } else if (letter == '[' && state == State.Text)
                {
                    state = State.Tag;
                } else if (letter == ']' && state == State.Tag)
                {
                    state = State.Text;
                }
                else if (state == State.Text || state == State.Slash)
                {
                    state = State.Text;
                    result.Append(letter);
                }
            }

            return result.ToString();
        }

        public static string ToSqlEscapeString(this string str)
        {
            return "\"" +  str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        public static string ToEnumName(this string str)
        {
            var sb = new StringBuilder();

            foreach (var c in str)
            {
                if (char.IsLetter(c) || char.IsDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
                else if (c == ' ')
                    sb.Append('_');
            }

            return sb.ToString();
        }

        private enum State
        {
            Text,
            Slash,
            Tag
        }
    }
}