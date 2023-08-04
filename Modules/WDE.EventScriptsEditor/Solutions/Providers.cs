using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.EventScriptsEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.Solutions;

[AutoRegister]
[SingleInstance]
public class Providers : ISolutionNameProvider<EventScriptSolutionItem>,
    ISolutionItemEditorProvider<EventScriptSolutionItem>,
    ISolutionItemIconProvider<EventScriptSolutionItem>
{
    private readonly IContainerProvider ioc;
    private readonly ISpellStore spellStore;
    private readonly ICachedDatabaseProvider databaseProvider;

    public Providers(IContainerProvider ioc,
        ISpellStore spellStore,
        ICachedDatabaseProvider databaseProvider)
    {
        this.ioc = ioc;
        this.spellStore = spellStore;
        this.databaseProvider = databaseProvider;
    }
    
    public string GetName(EventScriptSolutionItem item)
    {
        switch (item.ScriptType)
        {
            case EventScriptType.Event:
                return $"Event {item.Id} script";
            case EventScriptType.Spell:
            {
                if (spellStore.HasSpell(item.Id))
                    return $"Spell {spellStore.GetName(item.Id)} ({item.Id}) script";
                else
                    return $"Spell {item.Id} script";
            }
            case EventScriptType.Waypoint:
                return $"Waypoints action {item.Id} script";
            case EventScriptType.QuestStart:
                var questStart = databaseProvider.GetCachedQuestTemplate(item.Id);
                var questStartName = questStart == null ? $"Quest {item.Id}" : questStart.Name + " (" + item.Id + ")";
                return questStartName + " start script";
            case EventScriptType.QuestEnd:
                var questEnd = databaseProvider.GetCachedQuestTemplate(item.Id);
                var questEndName = questEnd == null ? $"Quest {item.Id}" : questEnd.Name + " (" + item.Id + ")";
                return questEndName + " end script";
            case EventScriptType.Gossip:
                return "Gossip " + item.Id + " script";
            case EventScriptType.GameObjectUse:
                var gameobject = databaseProvider.GetCachedGameObjectTemplate(item.Id);
                var name = gameobject == null ? $"Gameobject {item.Id}" : gameobject.Name + " (" + item.Id + ")";
                return "On " + name + " use script";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IDocument GetEditor(EventScriptSolutionItem item)
    {
        return ioc.Resolve<EventScriptViewerViewModel>((typeof(EventScriptSolutionItem), item));
    }

    public ImageUri GetIcon(EventScriptSolutionItem icon)
    {
        return new ImageUri("Icons/document_event_script.png");
    }
}

[AutoRegister]
[SingleInstance]
public class EventScriptProvider : EventScriptBaseProvider
{
    public EventScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.Event;
}

[AutoRegister]
[SingleInstance]
public class WaypointScriptProvider : EventScriptBaseProvider
{
    public WaypointScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.Waypoint;
}

[AutoRegister]
[SingleInstance]
public class SpellScriptProvider : EventScriptBaseProvider
{
    public SpellScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.Spell;
}

[AutoRegister]
[SingleInstance]
public class QuestStartScriptProvider : EventScriptBaseProvider
{
    public QuestStartScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.QuestStart;
}

[AutoRegister]
[SingleInstance]
public class QuestEndScriptProvider : EventScriptBaseProvider
{
    public QuestEndScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.QuestEnd;
}

[AutoRegister]
[SingleInstance]
public class GossipScriptProvider : EventScriptBaseProvider
{
    public GossipScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }

    protected override EventScriptType Type => EventScriptType.Gossip;
}

[AutoRegister]
[SingleInstance]
public class GameObjectUseScriptProvider : EventScriptBaseProvider
{
    public GameObjectUseScriptProvider(IParameterFactory parameterFactory, IParameterPickerService pickerService) : base(parameterFactory, pickerService)
    {
    }
    
    protected override EventScriptType Type => EventScriptType.GameObjectUse;
}

public abstract class EventScriptBaseProvider : ISolutionItemProvider
{
    protected abstract EventScriptType Type { get; }
    private readonly IParameterFactory parameterFactory;
    private readonly IParameterPickerService pickerService;

    public EventScriptBaseProvider(IParameterFactory parameterFactory,
        IParameterPickerService pickerService)
    {
        this.parameterFactory = parameterFactory;
        this.pickerService = pickerService;
    }
    
    public bool ByDefaultHideFromQuickStart => true;

    public string GetName() => Type.ToString() + " Script";

    public ImageUri GetImage() => new ImageUri("Icons/document_event_script_big.png");

    public string GetDescription() => "Legacy scripting system. Use only to check if a script exists and what does it have.";

    public string GetGroupName() => "Event scripts";

    public bool IsCompatibleWithCore(ICoreVersion core)
    {
        return core.SupportedEventScripts.HasFlagFast(Type);
    }

    public async Task<ISolutionItem?> CreateSolutionItem()
    {
        var param = parameterFactory.Factory(Type == EventScriptType.Spell ? "SpellParameter" : "Parameter");
        var pick = await pickerService.PickParameter(param, 0);
        if (pick.ok && uint.TryParse(pick.value.ToString(), out _))
            return new EventScriptSolutionItem(Type, (uint)pick.value);
        return null;
    }
}