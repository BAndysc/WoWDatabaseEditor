using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;

namespace WDE.MangosEventAiEditor;

public abstract class EventAiSolutionItemProvider : ISolutionItemProvider
{
    private readonly string desc;
    private readonly ImageUri icon;
    private readonly string name;

    protected EventAiSolutionItemProvider(string name, string desc, string icon)
    {
        this.name = name;
        this.desc = desc;
        this.icon = new ImageUri($"Icons/{icon}.png");
    }

    public string GetName()
    {
        return name;
    }

    public ImageUri GetImage()
    {
        return icon;
    }

    public string GetDescription()
    {
        return desc;
    }
        
    public string GetGroupName() => "Event AI";

    public bool IsCompatibleWithCore(ICoreVersion core)
    {
        return true;
    }

    public abstract Task<ISolutionItem?> CreateSolutionItem();
    public abstract Task<IReadOnlyCollection<ISolutionItem>> CreateMultipleSolutionItems();
}