using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Parameters;

namespace WDE.EventAiEditor.Parameters;

public class MangosEmoteParameter : IParameter<long>
{
    private readonly IParameter<long> emoteOneShot;
    private readonly IParameter<long> emoteState;

    public MangosEmoteParameter(IParameter<long> emoteOneShot, IParameter<long> emoteState)
    {
        this.emoteOneShot = emoteOneShot;
        this.emoteState = emoteState;
        Items = emoteOneShot.Items!
            .Concat(emoteState.Items!)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public string? Prefix { get; set; }
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    
    public string ToString(long value)
    {
        if (value == 0)
            return "Reset emote state";
        if (emoteOneShot.Items?.TryGetValue(value, out var oneShot) ?? false)
            return $"Play one shot emote [p=0]{oneShot.Name}[/p]";
        if (emoteState.Items?.TryGetValue(value, out var state) ?? false)
            return $"Set emote state to [p=0]{state.Name}[/p]";
        return $"Play emote [p=0]{value}[/p]";
    }

    public Dictionary<long, SelectOption>? Items { get; private set; }
}