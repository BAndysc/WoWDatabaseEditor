using System;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models;

public abstract class VisualSmartBaseElement : SmartBaseElement
{
    protected VisualSmartBaseElement(int id,
        ParametersCount parametersCount, 
        Func<SmartBaseElement, ParameterValueHolder<long>> paramCreator) : base(id, parametersCount, paramCreator)
    {
    }
    
    // while it breaks a single responsibility principle, it makes the code work way faster by caching it here
    // without using any additional Dictionaries, so for sake of performance, let's keep it this way
    public double? CachedHeight { get; set; }
    public PositionSize Position { get; set; }

    private (int kind, long entry) colorId;
    public (int kind, long entry) ColorId
    {
        get => colorId;
        set
        {
            colorId = value;
            OnPropertyChanged();
        }
    }

    protected override void CallOnChanged(object? sender)
    {
        CachedHeight = null;
        base.CallOnChanged(sender);
    }
}