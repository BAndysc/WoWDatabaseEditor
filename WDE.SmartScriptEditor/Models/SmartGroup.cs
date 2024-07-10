using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models;
public class SmartGroup
{
    private const int ParameterIsExpanded = 0;
    
    private readonly SmartEvent e;

    public bool IsSelected
    {
        get => e.IsSelected;
        set => e.IsSelected = value;
    }

    public bool IsExpanded
    {
        get => e.GetParameter(ParameterIsExpanded).Value == 0;
        set => e.GetParameter(ParameterIsExpanded).Value = value ? 0 : 1;
    }

    public double? CachedHeight 
    { 
        get => e.CachedHeight;
        set => e.CachedHeight = value;
    }

    public PositionSize Position
    {
        get => e.Position;
        set => e.Position = value;
    } 
    
    public string Header
    {
        get => e.GetStringParameter(0).Value;
        set => e.GetStringParameter(0).Value = value;
    }

    public string? Description
    {
        get
        {
            var str = e.GetStringParameter(1).Value;
            if (string.IsNullOrWhiteSpace(str))
                return null;
            return str;
        }
        set => e.GetStringParameter(1).Value = value ?? "";
    }

    public SmartGroup(SmartEvent e)
    {
        this.e = e;
    }
    
    internal SmartEvent InnerEvent => e;
}

public class SmartGroupFakeEditorFeatures : IEditorFeatures
{
    public static SmartGroupFakeEditorFeatures Instance { get; } = new SmartGroupFakeEditorFeatures();
    
    public string Name => "group meta features";
    public bool SupportsSource => true;
    public bool SupportsEventCooldown => true;
    public bool SupportsTargetCondition  => true;
    public bool SupportsEventTimerId  => true;
    public bool SourceHasPosition => true;
    public ParametersCount ConditionParametersCount { get; } = new ParametersCount(0, 0, 0);
    public ParametersCount EventParametersCount { get; } = new ParametersCount(4, 0, 2);
    public ParametersCount ActionParametersCount { get; } = new ParametersCount(0, 0, 0);
    public ParametersCount TargetParametersCount { get; } = new ParametersCount(0, 0, 0);
    public IParameter<long> ConditionTargetParameter => Parameter.Instance;
    public IParameter<long> EventFlagsParameter => Parameter.Instance;
    public int TargetConditionId => -1;
    public int? NonBreakableLinkFlag => null;
}