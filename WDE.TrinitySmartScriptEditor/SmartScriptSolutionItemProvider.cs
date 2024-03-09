using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor
{
    public abstract class SmartScriptSolutionItemProvider : ISolutionItemProvider
    {
        private readonly string desc;
        private readonly ImageUri icon;
        private readonly string name;
        private readonly SmartScriptType type;

        protected SmartScriptSolutionItemProvider(string name, string desc, string icon, SmartScriptType type)
        {
            this.name = name;
            this.desc = desc;
            this.type = type;
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
        
        public string GetGroupName() => "Smart scripts";

        public bool ShowInQuickStart(ICoreVersion core) => 
            core.SmartScriptFeatures.ProposeSmartScriptOnMainPage;
        
        public bool IsCompatibleWithCore(ICoreVersion core) => 
            core.SmartScriptFeatures.SupportedTypes.Contains(type);

        public abstract Task<ISolutionItem?> CreateSolutionItem();
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptCreatureProvider : SmartScriptSolutionItemProvider, IRelatedSolutionItemCreator, INumberSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryOrGuidProviderService> creatureEntryProvider;

        public SmartScriptCreatureProvider(Lazy<ICreatureEntryOrGuidProviderService> creatureEntryProvider) : base("Creature Script",
            "Script any npc in game.",
            "document_creature_big",
            SmartScriptType.Creature)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            int? entry = await creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem(entry.Value, SmartScriptType.Creature);
        }
        
        public Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related)
        {
            return Task.FromResult<ISolutionItem?>(
                new SmartScriptSolutionItem((int)related.Entry, SmartScriptType.Creature));
        }

        public bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related)
        {
            return related.Type == RelatedSolutionItem.RelatedType.CreatureEntry;
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number)
        {
            return Task.FromResult<ISolutionItem?>(
                new SmartScriptSolutionItem((int)number, SmartScriptType.Creature));
        }

        public string ParameterName => "CreatureParameter";
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptGameobjectProvider : SmartScriptSolutionItemProvider, IRelatedSolutionItemCreator, INumberSolutionItemProvider
    {
        private readonly Lazy<IGameobjectEntryOrGuidProviderService> goProvider;

        public SmartScriptGameobjectProvider(Lazy<IGameobjectEntryOrGuidProviderService> goProvider) : base("Gameobject Script",
            "Create script for object, including transports.",
            "document_gobject_big",
            SmartScriptType.GameObject)
        {
            this.goProvider = goProvider;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            int? entry = await goProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem(entry.Value, SmartScriptType.GameObject);
        }

        public Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related)
        {
            return Task.FromResult<ISolutionItem?>(
                new SmartScriptSolutionItem((int)related.Entry, SmartScriptType.GameObject));
        }

        public bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related)
        {
            return related.Type == RelatedSolutionItem.RelatedType.GameobjectEntry;
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number)
        {
            return Task.FromResult<ISolutionItem?>(
                new SmartScriptSolutionItem((int)number, SmartScriptType.GameObject));
        }

        public string ParameterName => "GameobjectParameter";
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptQuestProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IQuestEntryProviderService> service;

        public SmartScriptQuestProvider(Lazy<IQuestEntryProviderService> service) : base("Quest Script",
            "Write a script for quest: on accept, on reward.",
            "document_quest_big",
            SmartScriptType.Quest)
        {
            this.service = service;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            uint? entry = await service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Quest);
        }
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptAuraProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptAuraProvider(Lazy<ISpellEntryProviderService> service) : base("Aura Script",
            "Auras can have scripted several events: on apply, on remove, on periodic tick.",
            "document_aura_big",
            SmartScriptType.Aura)
        {
            this.service = service;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            uint? entry = await service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Aura);
        }
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptSpellProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptSpellProvider(Lazy<ISpellEntryProviderService> service) : base("Spell Script",
            "Create a new script for spell: this includes script for any existing effect in spell.",
            "document_spell_big",
            SmartScriptType.Spell)
        {
            this.service = service;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            uint? entry = await service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptTimedActionListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IDatabaseProvider> databaseProvider;
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;

        public SmartScriptTimedActionListProvider(Lazy<IDatabaseProvider> databaseProvider,
            Lazy<IItemFromListProvider> itemFromListProvider) : base(
            "Timed action list",
            "Timed action list contains list of actions played in time, this can be used to create RP events, cameras, etc.",
            "document_timedactionlist_big",
            SmartScriptType.TimedActionList)
        {
            this.databaseProvider = databaseProvider;
            this.itemFromListProvider = itemFromListProvider;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var list = await databaseProvider.Value.GetSmartScriptEntriesByType(SmartScriptType.TimedActionList);

            var items = list.ToDictionary(l => (long)l, l => new SelectOption("Timed action list"));

            long? entry = await itemFromListProvider.Value.GetItemFromList(items, false);
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.TimedActionList);
        }
    }
    
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptClientAreaTriggerEntityListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDbcStore> dbcStore;

        public SmartScriptClientAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDbcStore> dbcStore
        ) : base("Client Area Trigger",
            "The script from AreaTrigger from client database (DBC)",
            "document_areatrigger_big",
            SmartScriptType.AreaTrigger)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.dbcStore = dbcStore;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var areaTriggers =
                dbcStore.Value.AreaTriggerStore.ToDictionary(at => at.Key, at => new SelectOption($"Client areatrigger {at.Key}"));
            long? entry = await itemFromListProvider.Value.GetItemFromList(areaTriggers, false);
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.AreaTrigger);
        }
    }
    
    public abstract class SmartScriptAreaTriggerEntityListProviderBase : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDatabaseProvider> database;
        private readonly SmartScriptType type;
        private readonly bool serverSide;

        public SmartScriptAreaTriggerEntityListProviderBase(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database,
            string name,
            string desc,
            string icon,
            SmartScriptType type,
            bool serverSide
        ) : base(name, desc, icon, type)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.database = database;
            this.type = type;
            this.serverSide = serverSide;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var areaTriggers = (await database.Value.GetAreaTriggerTemplatesAsync())
                .Where(trigger => trigger.IsServerSide == serverSide)
                .ToDictionary(at => (long)at.Id, at => new SelectOption($"Area trigger {at.Id}"));

            long? entry = await itemFromListProvider.Value.GetItemFromList(areaTriggers, false);
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, type);
        }
    }
    
    // [AutoRegisterToParentScopeAttribute]
    public class SmartScriptAreaTriggerEntityListProvider : SmartScriptAreaTriggerEntityListProviderBase
    {
        public SmartScriptAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database
        ) : base(
            itemFromListProvider,
            database,
            "Area Trigger Entity",
            "The script from AreaTrigger defined in areatrigger_template",
            "document_areatrigger_big",
            SmartScriptType.AreaTriggerEntity,
            false) {}
    }
    
    // [AutoRegisterToParentScopeAttribute]
    public class SmartScriptServerSideAreaTriggerEntityListProvider : SmartScriptAreaTriggerEntityListProviderBase
    {
        public SmartScriptServerSideAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database
        ) : base(
            itemFromListProvider,
            database,
            "Area Trigger Server-side Entity",
            "The script from server side AreaTrigger defined in areatrigger_template",
            "document_areatrigger_big",
            SmartScriptType.AreaTriggerEntityServerSide,
            true) {}
    }

    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptSceneListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDbcStore> dbcStore;
        private readonly Lazy<IDatabaseProvider> databaseProvider;

        public SmartScriptSceneListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDbcStore> dbcStore,
            Lazy<IDatabaseProvider> databaseProvider
        ) : base("Scene Script",
            "The script from Scene from client database (DBC)",
            "document_cinematic_big",
            SmartScriptType.Scene)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.dbcStore = dbcStore;
            this.databaseProvider = databaseProvider;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var sceneTemplates = await databaseProvider.Value.GetSceneTemplatesAsync();
            Dictionary<long, SelectOption> scenes = new();
            if (sceneTemplates != null)
            {
                var sceneNameStore = dbcStore.Value.SceneStore;
                foreach (var sceneTemplate in sceneTemplates)
                {
                    if (sceneNameStore.TryGetValue(sceneTemplate.ScriptPackageId, out var name))
                        scenes.Add(sceneTemplate.SceneId, new SelectOption($"{name} ({sceneTemplate.ScriptPackageId})"));
                    else
                        scenes.Add(sceneTemplate.SceneId, new SelectOption($"unknown name ({sceneTemplate.ScriptPackageId})"));
                }
            }
            long? entry = await itemFromListProvider.Value.GetItemFromList(scenes, false);
            if (!entry.HasValue)
                return null;

            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Scene);
        }
    }
}