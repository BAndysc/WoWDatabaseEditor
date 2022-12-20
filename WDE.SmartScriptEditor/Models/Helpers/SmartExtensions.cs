namespace WDE.SmartScriptEditor.Models.Helpers;

public static class SmartExtensions
{
    public static long GetValueOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.ParametersCount)
            return element.GetParameter(parameter).Value;
        return default;
    }
    
    public static float GetFloatValueOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.FloatParametersCount)
            return element.GetFloatParameter(parameter).Value;
        return default;
    }
    
    public static string? GetStringValueOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.StringParametersCount)
            return element.GetStringParameter(parameter).Value;
        return default;
    }
    
    
    public static string? GetTextOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.ParametersCount)
            return element.GetParameter(parameter).ToString();
        return default;
    }
    
    public static string? GetFloatTextOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.FloatParametersCount)
            return element.GetFloatParameter(parameter).ToString();
        return default;
    }
    
    public static string? GetStringTextOrDefault(this SmartBaseElement element, int parameter)
    {
        if (parameter >= 0 && parameter < element.StringParametersCount)
            return element.GetStringParameter(parameter).ToString();
        return default;
    }
}