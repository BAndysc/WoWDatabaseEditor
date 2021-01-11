using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WDE.Common;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor
{
    public abstract class SmartScriptSolutionItemProvider : ISolutionItemProvider
    {
        private readonly string desc;
        private readonly ImageSource icon;
        private readonly string name;
        private readonly SmartScriptType type;

        protected SmartScriptSolutionItemProvider(string name, string desc, string icon, SmartScriptType type)
        {
            this.name = name;
            this.desc = desc;
            this.type = type;
            this.icon = new BitmapImage(new Uri($"/WDE.SmartScriptEditor;component/Resources/{icon}.png",
                UriKind.Relative));
        }

        public string GetName() { return name; }

        public ImageSource GetImage() { return icon; }

        public string GetDescription() { return desc; }

        public abstract ISolutionItem CreateSolutionItem();
    }

    [AutoRegister]
    public class SmartScriptCreatureProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProvider;

        public SmartScriptCreatureProvider(Lazy<ICreatureEntryProviderService> creatureEntryProvider)
            : base("Creature Script", "Script any npc in game.", "SmartScriptCreatureIcon", SmartScriptType.Creature)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Creature);
        }
    }

    [AutoRegister]
    public class SmartScriptGameobjectProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IGameobjectEntryProviderService> goProvider;

        public SmartScriptGameobjectProvider(Lazy<IGameobjectEntryProviderService> goProvider)
            : base("Gameobject Script",
                "Create script for object, including transports.",
                "SmartScriptGameobjectIcon",
                SmartScriptType.GameObject)
        {
            this.goProvider = goProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = goProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.GameObject);
        }
    }

    [AutoRegister]
    public class SmartScriptQuestProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IQuestEntryProviderService> service;

        public SmartScriptQuestProvider(Lazy<IQuestEntryProviderService> service)
            : base("Quest Script",
                "Write a script for quest: on accept, on reward.",
                "SmartScriptQuestIcon",
                SmartScriptType.Quest)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Quest);
        }
    }

    [AutoRegister]
    public class SmartScriptAuraProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptAuraProvider(Lazy<ISpellEntryProviderService> service)
            : base("Aura Script",
                "Auras can have scripted several events: on apply, on remove, on periodic tick.",
                "SmartScriptAuraIcon",
                SmartScriptType.Aura)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptSpellProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptSpellProvider(Lazy<ISpellEntryProviderService> service)
            : base("Spell Script",
                "Create a new script for spell: this includes script for any existing effect in spell.",
                "SmartScriptSpellIcon",
                SmartScriptType.Spell)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptTimedActionListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProvider;

        public SmartScriptTimedActionListProvider(Lazy<ICreatureEntryProviderService> creatureEntryProvider)
            : base("Timed action list",
                "Timed action list contains list of actions played in time, this can be used to create RP events, cameras, etc.",
                "SmartScriptTimedActionListIcon",
                SmartScriptType.TimedActionList)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.TimedActionList);
        }
    }
}