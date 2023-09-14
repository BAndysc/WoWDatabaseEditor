using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Parameters;

public class MultiStringParameter : IParameter<string>, ICustomPickerParameter<string>
{
    private readonly IParameter<long> source;
    private readonly IParameterPickerService parameterPickerService;

    public MultiStringParameter(IParameter<long> source,
        IParameterPickerService parameterPickerService)
    {
        this.source = source;
        this.parameterPickerService = parameterPickerService;
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public Dictionary<string, SelectOption>? Items => null;
        
    public string ToString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
            
        var elements = value.Split(' ');
        if (elements.Length == 1)
        {
            if (long.TryParse(elements[0], out var l))
                return source.ToString(l);
        }
        else
        {
            StringBuilder sb = new StringBuilder();

            foreach (var key in elements)
            {
                if (long.TryParse(key, out var l))
                    sb.Append(source.ToString(l)).Append(' ');
                else
                {
                    sb.Append(key);
                    sb.Append(' ');
                }
            }
                
            return sb.ToString();
        }
        return value;
    }

    public async Task<(string, bool)> PickValue(string value)
    {
        var currentSpells = value.Split(' ').Where(x => long.TryParse(x, out _)).Select(long.Parse).ToList();
        var pickedSpells = await parameterPickerService.PickMultipleSimple(currentSpells, source);
        if (pickedSpells != null)
            return (string.Join(" ", pickedSpells), true);
        return ("", false);
    }
}