using System;
using System.Linq;
using System.Text;

namespace WDE.Common.Utils
{
    public static class StringExtensions
    {
        public static string? NullIfEmpty(this string? s)
        {
            if (string.IsNullOrEmpty(s))
                return null;
            return s;
        }

        public static string ToHumanFriendlyFileSize(this ulong size)
        {
            if (size < 1024)
                return size + " B";
            var s = (float)size;
            s /= 1024.0f;
            if (s < 1024)
                return s.ToString("0.00") + " KB";
            s /= 1024.0f;
            if (s < 1024)
                return s.ToString("0.00") + " MB";
            s /= 1024.0f;
            return s.ToString("0.00") + " GB";
        }
    
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

        public static string ToAlphanumerical(this string str)
        {
            char[] arr = str.Where(c => (char.IsLetterOrDigit(c) || 
                                         char.IsWhiteSpace(c) || 
                                         c == '-')).ToArray(); 

            return new string(arr);
        }
        
        public static string ToEnumName(this string str)
        {
            var sb = new StringBuilder();

            foreach (var c in str)
            {
                if (char.IsLetter(c) || char.IsDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
                else if ((c == ' ' || c == '_') && sb.Length == 0 || sb[^1] != '_')
                    sb.Append('_');
            }

            return sb.ToString();
        }

        public static string TrimToLength(this string str, int length)
        {
            if (str.Length < length + 8)
                return str;
            return str.Substring(0, length) + "...";
        }
        
        private enum State
        {
            Text,
            Slash,
            Tag
        }

        public static string ToTitleCase(this string str)
        {
            StringBuilder sb = new();

            bool previousWasSeparator = true;
            bool previousWasLetter = false;
            bool previousWasLowerLetter = false;
            bool previousWasDigit = false;

            foreach (var c in str)
            {
                char chr = c;
                if (chr == '_')
                    chr = ' ';

                if (Char.IsDigit(c) && previousWasLetter)
                    sb.Append(' ');

                if (Char.IsUpper(c) && previousWasLowerLetter)
                    sb.Append(' ');
                
                if (previousWasSeparator)
                {
                    sb.Append(char.ToUpper(chr));
                    previousWasSeparator = false;
                }
                else
                    sb.Append(chr);
                
                
                if (chr == ' ')
                    previousWasSeparator = true;
                previousWasDigit = Char.IsDigit(c);
                previousWasLetter = Char.IsLetter(c);
                previousWasLowerLetter = previousWasLetter && Char.IsLower(c);
            }

            return sb.ToString();
        }

        public static string ToPrettyDuration(this int milliseconds)
        {
            if (milliseconds == 0)
                return "0 ms";
            int seconds = milliseconds / 1000;
            int minutes = seconds / 60;
            int hours = minutes / 60;
            int days = hours / 24;
            hours %= 24;
            minutes %= 60;
            seconds %= 60;
            milliseconds %= 1000;
            StringBuilder sb = new();
            if (hours > 0)
                sb.Append($"{hours}h ");
            if (minutes > 0)
                sb.Append($"{minutes}m ");
            if (seconds > 0)
                sb.Append($"{seconds}s ");
            if (milliseconds > 0)
                sb.Append($"{milliseconds}ms");
            return sb.ToString();
        }

        private static bool TryConsumeWordInternal(this string s, ref int startIndex, out ReadOnlySpan<char> str)
        {
            var nextSpace = s.IndexOf(' ', startIndex);
            if (nextSpace == -1)
            {
                str = s.AsSpan(startIndex);
                return true;
            }
            else
            {
                str = s.AsSpan(startIndex, nextSpace - startIndex);
                startIndex = nextSpace + 1;
                return true;
            }
        }

        public static bool TryConsumeWord(this string s, ref int startIndex, out string str)
        {
            if (s.TryConsumeWordInternal(ref startIndex, out var span))
            {
                str = span.ToString();
                return true;
            }
            else
            {
                str = "";
                return false;
            }
        }

        public static bool TryConsumeInt(this string s, ref int startIndex, out int number)
        {
            number = 0;
            return s.TryConsumeWordInternal(ref startIndex, out var word) && int.TryParse(word, out number);
        }

        public static bool TryConsumeUInt(this string s, ref int startIndex, out uint number)
        {
            number = 0;
            return s.TryConsumeWordInternal(ref startIndex, out var word) && uint.TryParse(word, out number);
        }

        public static bool TryConsumeLong(this string s, ref int startIndex, out long number)
        {
            number = 0;
            return s.TryConsumeWordInternal(ref startIndex, out var word) && long.TryParse(word, out number);
        }
    }
}