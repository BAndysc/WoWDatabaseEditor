using System;
using SmartFormat.Core.Extensions;

namespace WDE.SmartScriptEditor.Utils;

public class HasFlagFormatter : IFormatter
{
    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        if (formattingInfo.CurrentValue is not long l)
            return false;

        if (formattingInfo.FormatterOptions is not { } s)
            return false;
        
        if (!long.TryParse(s, out var flagToCheck))
            throw new Exception("hasflag formatter: cannot parse flag to check: expected a number, got " + s);

        if (formattingInfo.Format == null)
            return true;
        
        var formats = formattingInfo.Format.Split('|');

        if (formats == null) 
            return true;

        if ((l & flagToCheck) == 0)
        {
            if (formats.Count > 1)
                formattingInfo.Write(formats[1], formattingInfo.CurrentValue);
        }
        else
        {
            if (formats.Count > 0)
                formattingInfo.Write(formats[0], formattingInfo.CurrentValue);
        }

        return true;
    }

    private string[] names = new string[] {"hasflag"};
    public string[] Names {
        get
        {
            return names;
        }
        set {} 
    }
}