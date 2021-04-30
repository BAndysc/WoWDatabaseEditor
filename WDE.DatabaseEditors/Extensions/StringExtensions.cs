using System;
using System.Text;

namespace WDE.DatabaseEditors.Extensions
{
    public static class StringExtensions
    {
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
    }
}