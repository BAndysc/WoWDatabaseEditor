using System;
using System.Text;

namespace WDE.SqlWorkbench.Utils;

public static class MySqlHelperExtras
{
    public static string UnescapeString(this string value, int start = 0, int end = -1)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (end == -1)
            end = value.Length;
        
        StringBuilder sb = new StringBuilder(value.Length);
        bool escapeNext = false;

        for (var index = start; index < end; index++)
        {
            var c = value[index];
            if (escapeNext)
            {
                sb.Append(c);
                escapeNext = false;
            }
            else if (c == '\\')
            {
                escapeNext = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}