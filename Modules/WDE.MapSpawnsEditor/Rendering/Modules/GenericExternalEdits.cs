using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.EventAiEditor;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.SmartScriptEditor;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

internal class GenericExternalEdits : IMapSpawnModule
{
    private readonly IEventAggregator eventAggregator;
    private bool hasEventAi;
    private bool hasCreatureSmartScript;
    private bool hasGameObjectSmartScript;
    
    public GenericExternalEdits(ICurrentCoreVersion currentCoreVersion,
        IEventAggregator eventAggregator)
    {
        this.eventAggregator = eventAggregator;
        var core = currentCoreVersion.Current;
        hasEventAi = core.EventAiFeatures.IsSupported;
        hasCreatureSmartScript = core.SmartScriptFeatures.SupportedTypes.Contains(SmartScriptType.Creature);
        hasGameObjectSmartScript = core.SmartScriptFeatures.SupportedTypes.Contains(SmartScriptType.GameObject);
    }

    public bool CanEditCreature => hasEventAi || hasCreatureSmartScript;
    public bool CanEditGameObject => hasGameObjectSmartScript;

    public void OpenScript(SpawnInstance spawn)
    {
        if (spawn is CreatureSpawnInstance cr)
            OpenScript(cr);
        else if (spawn is GameObjectSpawnInstance go)
            OpenScript(go);
    }

    public void OpenSpawnScript(SpawnInstance spawn)
    {
        if (spawn is CreatureSpawnInstance cr)
            OpenSpawnScript(cr);
        else if (spawn is GameObjectSpawnInstance go)
            OpenSpawnScript(go);
    }
    
    public void OpenScript(CreatureSpawnInstance creature)
    {
        if (hasCreatureSmartScript)
            Open(new SmartScriptSolutionItem((int)creature.Entry, SmartScriptType.Creature));
        else if (hasEventAi)
            Open(new EventAiSolutionItem((int)creature.Entry));
    }
    
    public void OpenScript(GameObjectSpawnInstance gob)
    {
        if (hasGameObjectSmartScript)
            Open(new SmartScriptSolutionItem((int)gob.Entry, SmartScriptType.GameObject));
    }
    
    public void OpenSpawnScript(CreatureSpawnInstance creature)
    {
        if (hasCreatureSmartScript)
            Open(new SmartScriptSolutionItem(-(int)creature.Guid, SmartScriptType.Creature));
        else if (hasEventAi)
            Open(new EventAiSolutionItem(-(int)creature.Guid));
    }
    
    public void OpenSpawnScript(GameObjectSpawnInstance gob)
    {
        if (hasGameObjectSmartScript)
            Open(new SmartScriptSolutionItem(-(int)gob.Guid, SmartScriptType.GameObject));
    }

    private void Open(ISolutionItem item)
    {
        eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }
}