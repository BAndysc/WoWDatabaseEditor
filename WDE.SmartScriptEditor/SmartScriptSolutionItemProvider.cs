using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WDE.Common;
using WDE.Common.Database;
using Prism.Ioc;
using WDE.Common.DBC;
using WDE.SmartScriptEditor.Data;
using Prism.Events;
using WDE.Common.Attributes;

namespace WDE.SmartScriptEditor
{
    public abstract class SmartScriptSolutionItemProvider : ISolutionItemProvider
    {
        private readonly string _name;
        private readonly string _desc;
        private readonly ImageSource _icon;
        private readonly SmartScriptType _type;
        
        protected SmartScriptSolutionItemProvider(string name, string desc, string icon, SmartScriptType type)
        {
            _name = name;
            _desc = desc;
            _type = type;
            _icon = new BitmapImage(new Uri($"/WDE.SmartScriptEditor;component/Resources/{icon}.png", UriKind.Relative));
        }

        public string GetName()
        {
            return _name;
        }

        public ImageSource GetImage()
        {
            return _icon;
        }

        public string GetDescription()
        {
            return _desc;
        }

        public abstract ISolutionItem CreateSolutionItem();

        //public virtual ISolutionItem CreateSolutionItem()
        //{
        //    return new SmartScriptSolutionItem(0, _type, null);
        //}
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
            uint? entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Creature);
        }
    }

    [AutoRegister]
    public class SmartScriptGameobjectProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IGameobjectEntryProviderService> goProvider;

        public SmartScriptGameobjectProvider(Lazy<IGameobjectEntryProviderService> goProvider)
            : base("Gameobject Script", "Create script for object, including transports.", "SmartScriptGameobjectIcon", SmartScriptType.GameObject)
        {
            this.goProvider = goProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = goProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.GameObject);
        }
    }

    [AutoRegister]
    public class SmartScriptQuestProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IQuestEntryProviderService> service;

        public SmartScriptQuestProvider(Lazy<IQuestEntryProviderService> service)
            : base("Quest Script", "Write a script for quest: on accept, on reward.", "SmartScriptQuestIcon", SmartScriptType.Quest)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Quest);
        }
    }

    [AutoRegister]
    public class SmartScriptAuraProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptAuraProvider(Lazy<ISpellEntryProviderService> service)
            : base("Aura Script", "Auras can have scripted several events: on apply, on remove, on periodic tick.", "SmartScriptAuraIcon", SmartScriptType.Aura)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptSpellProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptSpellProvider(Lazy<ISpellEntryProviderService> service)
            : base("Spell Script", "Create a new script for spell: this includes script for any existing effect in spell.", "SmartScriptSpellIcon", SmartScriptType.Spell)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptTimedActionListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProvider;
        public SmartScriptTimedActionListProvider(Lazy<ICreatureEntryProviderService> creatureEntryProvider)
            : base("Timed action list", "Timed action list contains list of actions played in time, this can be used to create RP events, cameras, etc.", "SmartScriptTimedActionListIcon", SmartScriptType.TimedActionList)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.TimedActionList);
        }
    }
}
